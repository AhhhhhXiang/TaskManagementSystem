using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagement.Core.Repository;
using TaskManagementAPI.Models.ProjectTask;
using TaskManagement.Data.Migrations.Models;
using TaskManagement.Core.Repository.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace TaskManagementAPI.Controllers.ProjectTask
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectTaskController : ControllerBase
    {
        private readonly ITaskManagementClient _client;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public ProjectTaskController(ITaskManagementClient client, UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _client = client;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<JsonResult> GetAll([FromQuery] string? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IdentityUser? user = null;
            bool isAdmin = false;

            if (!string.IsNullOrEmpty(userId))
            {
                user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
                }
            }

            IEnumerable<TaskManagement.Data.Migrations.Models.ProjectTask> baseQuery;

            if (!string.IsNullOrEmpty(projectId))
            {
                if (!Guid.TryParse(projectId, out var projectGuid))
                {
                    return new JsonResult(new { success = false, message = "Invalid projectId format." });
                }

                if (!isAdmin)
                {
                    if (string.IsNullOrEmpty(userId))
                        return new JsonResult(new { success = false, message = "User not found / not authenticated." });

                    var isProjectMember = _client.ProjectUserRepository
                        .GetAll()
                        .Any(pu => pu.ProjectId == projectGuid && pu.UserId == Guid.Parse(userId));

                    if (!isProjectMember)
                        return new JsonResult(new { success = false, message = "Access denied. You are not part of this project." });
                }

                baseQuery = _client.ProjectTaskRepository
                    .GetAll()
                    .Where(t => t.ProjectId == projectGuid);
            }
            else
            {
                if (!isAdmin)
                {
                    if (string.IsNullOrEmpty(userId))
                        return new JsonResult(new { success = false, message = "User not found / not authenticated." });

                    var userProjectIds = _client.ProjectUserRepository
                        .GetAll()
                        .Where(pu => pu.UserId == Guid.Parse(userId))
                        .Select(pu => pu.ProjectId)
                        .ToList();

                    if (userProjectIds.Count == 0)
                    {
                        return new JsonResult(new GetAllProjectTasksResponse
                        {
                            projectTasks = new List<ProjectTasksResponse>()
                        });
                    }

                    baseQuery = _client.ProjectTaskRepository
                        .GetAll()
                        .Where(t => userProjectIds.Contains(t.ProjectId));
                }
                else
                {
                    baseQuery = _client.ProjectTaskRepository.GetAll();
                }
            }

            var tasksList = baseQuery
                .OrderByDescending(t => t.CreatedDateTime)
                .Select(t => new ProjectTasksResponse
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    Title = t.Title,
                    Description = t.Description,
                    StartDate = t.StartDate,
                    DueDate = t.DueDate,
                    ProgressStatus = t.ProgressStatus,
                    PriorityStatus = t.priorityStatus,
                    CreatedBy = t.CreatedBy
                })
                .ToList();

            return new JsonResult(new GetAllProjectTasksResponse
            {
                projectTasks = tasksList
            });
        }


        [HttpGet("{projectTaskId}")]
        public async Task<JsonResult> Get(string projectTaskId)
        {
            if (!Guid.TryParse(projectTaskId, out var id))
                return new JsonResult(new { success = false, message = "Invalid projectTaskId format." });

            var task = _client.ProjectTaskRepository.GetById(id);
            if (task == null)
                return new JsonResult(new { success = false, message = "Project task not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");

            if (!isAdmin)
            {
                var isProjectMember = _client.ProjectUserRepository
                    .GetAll()
                    .Any(pu => pu.ProjectId == task.ProjectId && pu.UserId == Guid.Parse(userId));

                if (!isProjectMember)
                    return new JsonResult(new { success = false, message = "Access denied. You are not part of this project." });
            }

            return new JsonResult(new ProjectTasksResponse
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                Title = task.Title,
                Description = task.Description,
                StartDate = task.StartDate,
                DueDate = task.DueDate,
                ProgressStatus = task.ProgressStatus,
                PriorityStatus = task.priorityStatus,
                CreatedBy = task.CreatedBy
            });
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] CreateProjectTaskRequestForm form)
        {
            if (form.ProjectId == null || string.IsNullOrEmpty(form.Title))
                return new JsonResult(new { success = false, message = "ProjectId and Title are required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");

            if (!isAdmin)
            {
                var isProjectMember = _client.ProjectUserRepository
                    .GetAll()
                    .Any(pu => pu.ProjectId == form.ProjectId && pu.UserId == Guid.Parse(userId));

                if (!isProjectMember)
                    return new JsonResult(new { success = false, message = "Access denied. You are not part of this project." });
            }

            var newTask = new TaskManagement.Data.Migrations.Models.ProjectTask
            {
                ProjectId = form.ProjectId,
                Title = form.Title!,
                Description = form.Description,
                StartDate = form.StartDate,
                DueDate = form.DueDate,
                ProgressStatus = form.ProgressStatus,
                priorityStatus = form.PriorityStatus,
                status = 1,
                CreatedBy = userId,
                CreatedDateTime = DateTime.UtcNow.ToLocalTime()
            };

            _client.ProjectTaskRepository.Add(newTask);
            _client.ProjectTaskRepository.Save();

            return new JsonResult(new CreateProjectTaskResponse { projectTask = newTask });
        }

        [HttpPatch("{projectTaskId}")]
        public async Task<JsonResult> Update(string projectTaskId, [FromBody] UpdateProjectTaskRequestForm form)
        {
            if (!Guid.TryParse(projectTaskId, out var id))
                return new JsonResult(new { success = false, message = "Invalid projectTaskId format." });

            var existingTask = _client.ProjectTaskRepository.GetById(id);
            if (existingTask == null)
                return new JsonResult(new { success = false, message = "Project task not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");

            if (!isAdmin)
            {
                var isProjectMember = _client.ProjectUserRepository
                    .GetAll()
                    .Any(pu => pu.ProjectId == existingTask.ProjectId && pu.UserId == Guid.Parse(userId));

                if (!isProjectMember)
                    return new JsonResult(new { success = false, message = "Access denied. You are not part of this project." });
            }

            existingTask.Title = form.Title ?? existingTask.Title;
            existingTask.Description = form.Description ?? existingTask.Description;
            existingTask.StartDate = form.StartDate ?? existingTask.StartDate;
            existingTask.DueDate = form.DueDate ?? existingTask.DueDate;
            existingTask.ProgressStatus = form.ProgressStatus ?? existingTask.ProgressStatus;
            existingTask.priorityStatus = form.PriorityStatus ?? existingTask.priorityStatus;
            existingTask.UpdatedBy = userId;
            existingTask.UpdatedDateTime = DateTime.UtcNow.ToLocalTime();

            _client.ProjectTaskRepository.Update(existingTask);
            _client.ProjectTaskRepository.Save();

            return new JsonResult(new UpdateProjectTaskResponse
            {
                projectTask = new ProjectTaskModel
                {
                    Id = existingTask.Id,
                    ProjectId = existingTask.ProjectId,
                    Title = existingTask.Title,
                    Description = existingTask.Description,
                    StartDate = existingTask.StartDate,
                    DueDate = existingTask.DueDate,
                    ProgressStatus = existingTask.ProgressStatus,
                    PriorityStatus = existingTask.priorityStatus,
                    status = existingTask.status,
                    CreatedBy = existingTask.CreatedBy,
                    CreatedDateTime = existingTask.CreatedDateTime,
                    UpdatedBy = existingTask.UpdatedBy,
                    UpdatedDateTime = existingTask.UpdatedDateTime
                }
            });
        }

        [HttpDelete("{projectTaskId}")]
        public async Task<JsonResult> Delete(string projectTaskId)
        {
            if (!Guid.TryParse(projectTaskId, out var id))
                return new JsonResult(new { success = false, message = "Invalid projectTaskId format." });

            var existingTask = _client.ProjectTaskRepository.GetById(id);
            if (existingTask == null)
                return new JsonResult(new { success = false, message = "Project task not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");

            if (!isAdmin)
            {
                var isProjectMember = _client.ProjectUserRepository
                    .GetAll()
                    .Any(pu => pu.ProjectId == existingTask.ProjectId && pu.UserId == Guid.Parse(userId));

                if (!isProjectMember)
                    return new JsonResult(new { success = false, message = "Access denied. You are not part of this project." });
            }

            try
            {
                var taskAttachments = _client.TaskAttachmentRepository
                    .GetAll()
                    .Where(ta => ta.TaskId == id)
                    .ToList();

                foreach (var attachment in taskAttachments)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(attachment.FilePath))
                        {
                            var relativePath = attachment.FilePath.TrimStart('\\', '/');
                            var basePath = _configuration.GetSection("AttachmentPath:get").Value;
                            var folder = _configuration.GetSection("AttachmentPath:getAttachmentFolder").Value;
                            var fullPath = Path.Combine(basePath, folder, relativePath);

                            bool fileDeleted = _client.TaskAttachmentRepository.DeleteImage(fullPath);

                            if (!fileDeleted)
                            {
                                Console.WriteLine($"Warning: File deletion failed for {fullPath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting file for attachment {attachment.Id}: {ex.Message}");
                    }
                }

                var taskUsers = _client.TaskUserRepository
                    .GetAll()
                    .Where(tu => tu.TaskId == id)
                    .ToList();

                foreach (var taskUser in taskUsers)
                {
                    _client.TaskUserRepository.Delete(taskUser.Id);
                }
                _client.TaskUserRepository.Save();

                foreach (var attachment in taskAttachments)
                {
                    _client.TaskAttachmentRepository.Delete(attachment.Id);
                }
                _client.TaskAttachmentRepository.Save();

                _client.ProjectTaskRepository.Delete(existingTask.Id);
                _client.ProjectTaskRepository.Save();

                return new JsonResult(new
                {
                    success = true,
                    message = "Project task and all related data deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "An error occurred while deleting the project task."
                });
            }
        }
    }
}
