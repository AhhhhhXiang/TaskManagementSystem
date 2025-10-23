using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagement.Core.Repository;
using TaskManagementAPI.Models.TaskUser;

namespace TaskManagementAPI.Controllers.TaskUser
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskUserController : ControllerBase
    {
        private readonly ITaskManagementClient _client;
        private readonly UserManager<IdentityUser> _userManager;

        public TaskUserController(ITaskManagementClient client, UserManager<IdentityUser> userManager)
        {
            _client = client;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetAll([FromQuery] GetAllTaskUsersRequestForm form)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return new JsonResult(new { success = false, message = "User ID not found." });

            var userId = Guid.Parse(userIdStr);
            var query = _client.TaskUserRepository.GetAll().AsQueryable();

            if (!User.IsInRole("Administrator"))
            {
                var projectIds = _client.ProjectUserRepository
                    .GetAll()
                    .Where(pu => pu.UserId == userId)
                    .Select(pu => pu.ProjectId)
                    .ToList();

                var taskIds = _client.ProjectTaskRepository
                    .GetAll()
                    .Where(t => projectIds.Contains(t.ProjectId))
                    .Select(t => t.Id)
                    .ToList();

                query = query.Where(tu => taskIds.Contains(tu.TaskId));
            }

            if (form.TaskId.HasValue)
                query = query.Where(tu => tu.TaskId == form.TaskId.Value);
            else if (form.UserId.HasValue)
                query = query.Where(tu => tu.UserId == form.UserId.Value);

            var taskUsers = query.ToList();

            var responseList = taskUsers.Select(pu => new TaskUserResponse
            {
                Id = pu.Id,
                TaskId = pu.TaskId,
                UserId = pu.UserId
            }).ToList();

            return new JsonResult(new GetAllTaskUsersResponse
            {
                taskUsers = responseList
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> Create([FromBody] CreateTaskUserRequestForm form)
        {
            if (string.IsNullOrEmpty(form.TaskId) || string.IsNullOrEmpty(form.UserId))
                return new JsonResult(new { success = false, message = "Both TaskId and UserId are required." });

            if (!Guid.TryParse(form.TaskId, out var taskId) || !Guid.TryParse(form.UserId, out var userId))
                return new JsonResult(new { success = false, message = "Invalid TaskId or UserId format." });

            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr))
                return new JsonResult(new { success = false, message = "User ID not found." });

            var currentUserId = Guid.Parse(currentUserIdStr);
            var task = _client.ProjectTaskRepository.GetAll().FirstOrDefault(t => t.Id == taskId);

            if (task == null)
                return new JsonResult(new { success = false, message = "Task not found." });

            var projectId = task.ProjectId;

            var targetUserAssignedToProject = _client.ProjectUserRepository
                .GetAll()
                .Any(pu => pu.UserId == userId && pu.ProjectId == projectId);

            var currentUserAssignedToProject = _client.ProjectUserRepository
                .GetAll()
                .Any(pu => pu.UserId == currentUserId && pu.ProjectId == projectId);

            if (!User.IsInRole("Administrator") && !currentUserAssignedToProject)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "You are not authorized to assign users to this task."
                });
            }

            if (!targetUserAssignedToProject && !User.IsInRole("Administrator"))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "The selected user is not part of this project."
                });
            }

            var existing = _client.TaskUserRepository
                .GetAll()
                .FirstOrDefault(pu => pu.TaskId == taskId && pu.UserId == userId);

            if (existing != null)
                return new JsonResult(new { success = false, message = "User already assigned to this task." });

            var taskUser = new TaskManagement.Data.Migrations.Models.TaskUser
            {
                TaskId = taskId,
                UserId = userId,
                CreatedBy = currentUserIdStr,
                CreatedDateTime = DateTime.UtcNow,
                status = 1
            };

            _client.TaskUserRepository.Add(taskUser);
            _client.TaskUserRepository.Save();

            return new JsonResult(new CreateTaskUserResponse { taskUser = taskUser });
        }

        [Authorize]
        [HttpDelete("{taskUserId}")]
        public JsonResult Delete([FromRoute(Name = "taskUserId")] int taskUserId)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr))
                return new JsonResult(new { success = false, message = "User ID not found." });

            var currentUserId = Guid.Parse(currentUserIdStr);

            var taskUser = _client.TaskUserRepository
                .GetAll()
                .FirstOrDefault(pu => pu.Id == taskUserId);

            if (taskUser == null)
                return new JsonResult(new { success = false, message = "TaskUser not found." });

            var task = _client.ProjectTaskRepository
                .GetAll()
                .FirstOrDefault(t => t.Id == taskUser.TaskId);

            if (task == null)
                return new JsonResult(new { success = false, message = "Associated task not found." });

            var projectId = task.ProjectId;

            var userAssignedToProject = _client.ProjectUserRepository
                .GetAll()
                .Any(pu => pu.UserId == currentUserId && pu.ProjectId == projectId);

            if (!User.IsInRole("Administrator") && !userAssignedToProject)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "You are not authorized to delete this assignment."
                });
            }

            _client.TaskUserRepository.Delete(taskUser.Id);
            _client.TaskUserRepository.Save();

            return new JsonResult(new { success = true, message = "TaskUser deleted successfully." });
        }
    }
}
