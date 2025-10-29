using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagementAPI.Models.Project;
using TaskManagement.Data.Migrations.Models;
using TaskManagement.Core.Repository;
using TaskManagement.Core.Repository.Models;
using System.Linq;

namespace TaskManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly ITaskManagementClient _client;
        private readonly UserManager<IdentityUser> _userManager;

        public ProjectController(ITaskManagementClient client, UserManager<IdentityUser> userManager)
        {
            _client = client;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetAll([FromQuery] GetAllProjectsRequestForm form)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var projectsQuery = _client.ProjectRepository.GetAll().AsQueryable();

            // Filter by project name
            if (!string.IsNullOrEmpty(form.projectName))
            {
                projectsQuery = projectsQuery.Where(p => p.Name.ToLower().Contains(form.projectName.ToLower()));
            }

            // Filter by assigned member
            if (!string.IsNullOrEmpty(form.memberName))
            {
                var lowerMemberName = form.memberName.ToLower();

                var userProjectsByMember = _client.ProjectUserRepository
                    .GetAll()
                    .Where(pu => _userManager.Users
                        .Any(u => u.Id == pu.UserId.ToString() &&
                                 (u.UserName.ToLower().Contains(lowerMemberName) ||
                                  u.Email.ToLower().Contains(lowerMemberName))))
                    .Select(pu => pu.ProjectId)
                    .Distinct()
                    .ToList();

                var taskProjectsByMember = _client.TaskUserRepository
                    .GetAll()
                    .Where(tu => _userManager.Users
                        .Any(u => u.Id == tu.UserId.ToString() &&
                                 (u.UserName.ToLower().Contains(lowerMemberName) ||
                                  u.Email.ToLower().Contains(lowerMemberName))))
                    .Select(tu => _client.ProjectTaskRepository.GetById(tu.TaskId))
                    .Where(t => t != null)
                    .Select(t => t.ProjectId)
                    .Distinct()
                    .ToList();

                var allFilteredProjectIds = userProjectsByMember.Union(taskProjectsByMember).Distinct().ToList();

                projectsQuery = projectsQuery.Where(p => allFilteredProjectIds.Contains(p.Id));
            }

            // Filter by priority
            if (!string.IsNullOrEmpty(form.priority))
            {
                var lowerPriority = form.priority.ToLower();
                var priorityProjects = _client.ProjectTaskRepository
                    .GetAll()
                    .Where(t => t.priorityStatus.ToString().ToLower() == lowerPriority)
                    .Select(t => t.ProjectId)
                    .Distinct()
                    .ToList();

                projectsQuery = projectsQuery.Where(p => priorityProjects.Contains(p.Id));
            }

            if (userRole == "RegisterUser" && !string.IsNullOrEmpty(userId))
            {
                var userProjects = _client.ProjectUserRepository
                    .GetAll()
                    .Where(pu => pu.UserId == Guid.Parse(userId))
                    .Select(pu => pu.ProjectId)
                    .ToList();

                projectsQuery = projectsQuery.Where(p => userProjects.Contains(p.Id));
            }

            var totalCount = projectsQuery.Count();

            // Pagination
            int page = form.page > 0 ? form.page : 1;
            int pageSize = form.pageSize > 0 ? form.pageSize : 10;

            var pagedProjects = projectsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var responseProjects = new List<ProjectsResponse>();

            foreach (var project in pagedProjects)
            {
                var projectResponse = new ProjectsResponse
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description
                };

                if (form.modules != null && form.modules.Count > 0)
                {
                    if (form.modules.Contains("Tasks"))
                    {
                        var projectTasks = _client.ProjectTaskRepository
                            .GetAll()
                            .Where(pt => pt.ProjectId == project.Id)
                            .Select(pt =>
                            {
                                var task = _client.ProjectTaskRepository.GetById(pt.Id);
                                if (task == null) return null;

                                var taskReturnModel = new ProjectTaskReturnModel
                                {
                                    Id = task.Id,
                                    Title = task.Title,
                                    Description = task.Description,
                                    StartDate = task.StartDate,
                                    DueDate = task.DueDate,
                                    ProgressStatus = task.ProgressStatus,
                                    PriorityStatus = task.priorityStatus,
                                    CreatedBy = task.CreatedBy,
                                    CreatedDateTime = task.CreatedDateTime
                                };

                                if (form.modules.Contains("TaskUser"))
                                {
                                    var taskUsers = new List<UserReturnModel>();
                                    var taskUserLinks = _client.TaskUserRepository
                                        .GetAll()
                                        .Where(tu => tu.TaskId == task.Id)
                                        .ToList();

                                    foreach (var tu in taskUserLinks)
                                    {
                                        var user = _userManager.FindByIdAsync(tu.UserId.ToString()).Result;
                                        if (user != null)
                                        {
                                            taskUsers.Add(new UserReturnModel
                                            {
                                                Id = Guid.Parse(user.Id),
                                                UserName = user.UserName ?? "Unknown",
                                                Email = user.Email ?? "No email",
                                            });
                                        }
                                    }
                                    taskReturnModel.taskUsers = taskUsers;
                                }

                                if (form.modules.Contains("TaskAttachment"))
                                {
                                    var taskAttachments = _client.TaskAttachmentRepository
                                        .GetAll()
                                        .Where(ta => ta.TaskId == task.Id)
                                        .Select(ta => new TaskAttachmentReturnModel
                                        {
                                            Id = ta.Id,
                                            TaskId = ta.TaskId,
                                            FileName = ta.FileName,
                                            FilePath = Url.ActionLink(action: "GetAttachment", controller: "Attachment") + "?attachmentId=" + ta.Id
                                        })
                                        .ToList();

                                    taskReturnModel.taskAttachments = taskAttachments;
                                }

                                if (form.modules.Contains("TaskComment"))
                                {
                                    var taskComments = _client.TaskCommentRepository
                                        .GetAll()
                                        .Where(ta => ta.TaskId == task.Id)
                                        .ToList()
                                        .Select(ta =>
                                        {
                                            var user = _userManager.FindByIdAsync(ta.UserId.ToString()).Result;
                                            return new TaskCommentReturnModel
                                            {
                                                Id = ta.Id,
                                                TaskId = ta.TaskId,
                                                UserId = ta.UserId,
                                                Username = user?.UserName ?? "Unknown",
                                                Comment = ta.Comment,
                                                CreatedDateTime = ta.CreatedDateTime
                                            };
                                        })
                                        .ToList();

                                    taskReturnModel.taskComments = taskComments;
                                }

                                return taskReturnModel;
                            })
                            .Where(t => t != null)
                            .ToList();

                        projectResponse.projectTasks = projectTasks!;
                    }

                    if (form.modules.Contains("ProjectUser"))
                    {
                        var projectUsers = new List<ProjectUserReturnModel>();
                        var projectUserLinks = _client.ProjectUserRepository
                            .GetAll()
                            .Where(pu => pu.ProjectId == project.Id)
                            .ToList();

                        foreach (var pu in projectUserLinks)
                        {
                            var user = await _userManager.FindByIdAsync(pu.UserId.ToString());
                            if (user != null)
                            {
                                projectUsers.Add(new ProjectUserReturnModel
                                {
                                    Id = Guid.Parse(user.Id),
                                    UserName = user.UserName ?? "Unknown",
                                    Email = user.Email ?? "No email"
                                });
                            }
                        }

                        projectResponse.projectUsers = projectUsers;
                    }
                }

                responseProjects.Add(projectResponse);
            }

            var response = new GetAllProjectsResponse
            {
                projects = responseProjects,
                page = page,
                pageSize = pageSize,
                totalCount = totalCount
            };

            return new JsonResult(response);
        }

        [Authorize]
        [HttpGet("{projectId}")]
        public async Task<JsonResult> Get(Guid projectId, [FromQuery] GetProjectRequestForm form)
        {
            var project = _client.ProjectRepository.GetById(projectId);
            if (project == null)
                return new JsonResult(new { success = false, message = "Project not found" });

            var projectResponse = new ProjectsResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description
            };

            if (form.modules != null && form.modules.Count > 0)
            {
                if (form.modules.Contains("Tasks"))
                {
                    var tasksQuery = _client.ProjectTaskRepository
                        .GetAll()
                        .Where(pt => pt.ProjectId == project.Id)
                        .AsQueryable();

                    if (!string.IsNullOrEmpty(form.taskName))
                    {
                        tasksQuery = tasksQuery.Where(t => t.Title.ToLower().Contains(form.taskName.ToLower()));
                    }

                    if (form.taskStartDate.HasValue)
                    {
                        tasksQuery = tasksQuery.Where(t => t.DueDate >= form.taskStartDate.Value);
                    }

                    if (form.taskEndDate.HasValue)
                    {
                        tasksQuery = tasksQuery.Where(t => t.DueDate <= form.taskEndDate.Value);
                    }

                    if (!string.IsNullOrEmpty(form.taskPriority))
                    {
                        tasksQuery = tasksQuery.Where(t => t.priorityStatus.ToString().ToLower() == form.taskPriority.ToLower());
                    }

                    if (!string.IsNullOrEmpty(form.taskMemberName))
                    {
                        var lowerTaskMemberName = form.taskMemberName.ToLower();
                        var taskIdsByMember = _client.TaskUserRepository
                            .GetAll()
                            .Where(tu => _userManager.Users
                                .Any(u => u.Id == tu.UserId.ToString() &&
                                         (u.UserName.ToLower().Contains(lowerTaskMemberName) ||
                                          u.Email.ToLower().Contains(lowerTaskMemberName))))
                            .Select(tu => tu.TaskId)
                            .Distinct()
                            .ToList();

                        tasksQuery = tasksQuery.Where(t => taskIdsByMember.Contains(t.Id));
                    }

                    if (!string.IsNullOrEmpty(form.taskSortBy))
                    {
                        var isTaskDescending = form.taskSortOrder?.ToLower() == "desc";
                        tasksQuery = form.taskSortBy.ToLower() switch
                        {
                            "title" => isTaskDescending ?
                                tasksQuery.OrderByDescending(t => t.Title) :
                                tasksQuery.OrderBy(t => t.Title),
                            "duedate" => isTaskDescending ?
                                tasksQuery.OrderByDescending(t => t.DueDate) :
                                tasksQuery.OrderBy(t => t.DueDate),
                            "progressstatus" => isTaskDescending ?
                                tasksQuery.OrderByDescending(t => t.ProgressStatus) :
                                tasksQuery.OrderBy(t => t.ProgressStatus),
                            "prioritystatus" => isTaskDescending ?
                                tasksQuery.OrderByDescending(t => t.priorityStatus) :
                                tasksQuery.OrderBy(t => t.priorityStatus),
                            "createddatetime" => isTaskDescending ?
                                tasksQuery.OrderByDescending(t => t.CreatedDateTime) :
                                tasksQuery.OrderBy(t => t.CreatedDateTime),
                            _ => tasksQuery.OrderBy(t => t.Title)
                        };
                    }
                    else
                    {
                        tasksQuery = tasksQuery.OrderBy(t => t.Title);
                    }

                    int taskPage = form.taskPage > 0 ? form.taskPage : 1;
                    int taskPageSize = form.taskPageSize > 0 ? form.taskPageSize : 10;

                    var totalTaskCount = tasksQuery.Count();
                    var pagedTasks = tasksQuery
                        .Skip((taskPage - 1) * taskPageSize)
                        .Take(taskPageSize)
                        .ToList();

                    var projectTasks = pagedTasks.Select(task =>
                    {
                        var taskReturnModel = new ProjectTaskReturnModel
                        {
                            Id = task.Id,
                            Title = task.Title,
                            Description = task.Description,
                            StartDate = task.StartDate,
                            DueDate = task.DueDate,
                            ProgressStatus = task.ProgressStatus,
                            PriorityStatus = task.priorityStatus,
                            CreatedBy = task.CreatedBy,
                            CreatedDateTime = task.CreatedDateTime,
                            TotalTaskCount = totalTaskCount,
                            TaskPage = taskPage,
                            TaskPageSize = taskPageSize
                        };

                        if (form.modules.Contains("TaskUser"))
                        {
                            var taskUsers = new List<UserReturnModel>();
                            var taskUserLinks = _client.TaskUserRepository
                                .GetAll()
                                .Where(tu => tu.TaskId == task.Id)
                                .ToList();

                            foreach (var tu in taskUserLinks)
                            {
                                var user = _userManager.FindByIdAsync(tu.UserId.ToString()).Result;
                                if (user != null)
                                {
                                    taskUsers.Add(new UserReturnModel
                                    {
                                        Id = Guid.Parse(user.Id),
                                        UserName = user.UserName ?? "Unknown",
                                        Email = user.Email ?? "No email",
                                    });
                                }
                            }
                            taskReturnModel.taskUsers = taskUsers;
                        }

                        if (form.modules.Contains("TaskAttachment"))
                        {
                            var taskAttachments = _client.TaskAttachmentRepository
                                .GetAll()
                                .Where(ta => ta.TaskId == task.Id)
                                .Select(ta => new TaskAttachmentReturnModel
                                {
                                    Id = ta.Id,
                                    TaskId = ta.TaskId,
                                    FileName = ta.FileName,
                                    FilePath = Url.ActionLink(action: "GetAttachment", controller: "Attachment") + "?attachmentId=" + ta.Id,
                                })
                                .ToList();

                            taskReturnModel.taskAttachments = taskAttachments;
                        }

                        if (form.modules.Contains("TaskComment"))
                        {
                            var taskComments = _client.TaskCommentRepository
                                .GetAll()
                                .Where(ta => ta.TaskId == task.Id)
                                .ToList()
                                .Select(ta =>
                                {
                                    var user = _userManager.FindByIdAsync(ta.UserId.ToString()).Result;
                                    return new TaskCommentReturnModel
                                    {
                                        Id = ta.Id,
                                        TaskId = ta.TaskId,
                                        UserId = ta.UserId,
                                        Username = user?.UserName ?? "Unknown",
                                        Comment = ta.Comment,
                                        CreatedDateTime = ta.CreatedDateTime
                                    };
                                })
                                .ToList();

                            taskReturnModel.taskComments = taskComments;
                        }

                        return taskReturnModel;
                    }).ToList();

                    projectResponse.projectTasks = projectTasks;
                    projectResponse.TotalTaskCount = totalTaskCount;
                    projectResponse.TaskPage = taskPage;
                    projectResponse.TaskPageSize = taskPageSize;
                }

                if (form.modules.Contains("ProjectUser"))
                {
                    var projectUsers = new List<ProjectUserReturnModel>();
                    var projectUserLinks = _client.ProjectUserRepository
                        .GetAll()
                        .Where(pu => pu.ProjectId == project.Id)
                        .ToList();

                    foreach (var pu in projectUserLinks)
                    {
                        var user = await _userManager.FindByIdAsync(pu.UserId.ToString());
                        if (user != null)
                        {
                            projectUsers.Add(new ProjectUserReturnModel
                            {
                                Id = Guid.Parse(user.Id),
                                UserName = user.UserName ?? "Unknown",
                                Email = user.Email ?? "No email"
                            });
                        }
                    }

                    projectResponse.projectUsers = projectUsers;
                }
            }

            return new JsonResult(new GetProjectResponse
            {
                project = projectResponse
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> Create([FromBody] CreateProjectRequestForm form)
        {
            if (string.IsNullOrEmpty(form.Name))
                return new JsonResult(new { success = false, message = "Project name is required." });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return new JsonResult(new { success = false, message = "User ID not found." });

            var userId = Guid.Parse(userIdStr);

            var newProject = new Project
            {
                Id = Guid.NewGuid(),
                Name = form.Name,
                Description = form.Description,
                Remarks = form.Remarks,
                CreatedBy = userIdStr,
                CreatedDateTime = DateTime.UtcNow,
                status = 1
            };

            _client.ProjectRepository.Add(newProject);
            _client.ProjectRepository.Save();

            if (!User.IsInRole("Administrator"))
            {
                var projectUser = new ProjectUser
                {
                    ProjectId = newProject.Id,
                    UserId = userId,
                    CreatedBy = userIdStr,
                    CreatedDateTime = DateTime.UtcNow,
                    status = 1
                };

                _client.ProjectUserRepository.Add(projectUser);
                _client.ProjectUserRepository.Save();
            }

            return new JsonResult(new CreateProjectResposne
            {
                project = newProject
            });
        }

        [Authorize]
        [HttpPatch("{projectId}")]
        public JsonResult Update(Guid projectId, [FromBody] UpdateProjectRequestForm form)
        {
            if (projectId == Guid.Empty)
                return new JsonResult(new { success = false, message = "Invalid project ID." });

            var existingProject = _client.ProjectRepository.GetById(projectId);
            if (existingProject == null)
                return new JsonResult(new { success = false, message = "Project not found." });

            if (!string.IsNullOrEmpty(form.Name))
                existingProject.Name = form.Name;
            if (form.Description != null)
                existingProject.Description = form.Description;
            if (form.Remarks != null)
                existingProject.Remarks = form.Remarks;

            existingProject.UpdatedDateTime = DateTime.UtcNow;
            existingProject.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

            _client.ProjectRepository.Update(existingProject);
            _client.ProjectRepository.Save();

            var projectModel = new ProjectModel
            {
                Id = existingProject.Id,
                Name = existingProject.Name,
                Description = existingProject.Description,
                CreatedBy = existingProject.CreatedBy,
                CreatedDateTime = existingProject.CreatedDateTime,
                UpdatedBy = existingProject.UpdatedBy,
                UpdatedDateTime = existingProject.UpdatedDateTime,
                status = existingProject.status,
                Remarks = existingProject.Remarks
            };

            return new JsonResult(new UpdateProjectResponse
            {
                project = projectModel
            });
        }

        [Authorize]
        [HttpDelete("{projectId}")]
        public JsonResult Delete(Guid projectId)
        {
            if (projectId == Guid.Empty)
                return new JsonResult(new { success = false, message = "Invalid project ID." });

            var existingProject = _client.ProjectRepository.GetById(projectId);
            if (existingProject == null)
                return new JsonResult(new { success = false, message = "Project not found." });

            try
            {
                var projectTaskIds = _client.ProjectTaskRepository
                    .GetAll()
                    .Where(pt => pt.ProjectId == projectId)
                    .Select(pt => pt.Id)
                    .ToList();

                foreach (var taskId in projectTaskIds)
                {
                    var taskAttachments = _client.TaskAttachmentRepository
                        .GetAll()
                        .Where(ta => ta.TaskId == taskId)
                        .ToList();

                    foreach (var attachment in taskAttachments)
                    {
                        if (!string.IsNullOrEmpty(attachment.FilePath))
                        {
                            try
                            {
                                var relativePath = attachment.FilePath.TrimStart('\\', '/');

                                var fullPath = attachment.FilePath;

                                bool fileDeleted = _client.TaskAttachmentRepository.DeleteImage(fullPath);

                                if (!fileDeleted)
                                {
                                    Console.WriteLine($"Warning: File deletion failed for {fullPath}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error deleting file for attachment {attachment.Id}: {ex.Message}");
                            }
                        }

                        _client.TaskAttachmentRepository.Delete(attachment.Id);
                    }
                }
                _client.TaskAttachmentRepository.Save();

                foreach (var taskId in projectTaskIds)
                {
                    var taskUsers = _client.TaskUserRepository
                        .GetAll()
                        .Where(tu => tu.TaskId == taskId)
                        .ToList();

                    foreach (var taskUser in taskUsers)
                    {
                        _client.TaskUserRepository.Delete(taskUser.Id);
                    }
                }
                _client.TaskUserRepository.Save();

                var projectTasks = _client.ProjectTaskRepository
                    .GetAll()
                    .Where(pt => pt.ProjectId == projectId)
                    .ToList();

                foreach (var projectTask in projectTasks)
                {
                    _client.ProjectTaskRepository.Delete(projectTask.Id);
                }
                _client.ProjectTaskRepository.Save();

                var projectUsers = _client.ProjectUserRepository
                    .GetAll()
                    .Where(pu => pu.ProjectId == projectId)
                    .ToList();

                foreach (var projectUser in projectUsers)
                {
                    _client.ProjectUserRepository.Delete(projectUser.Id);
                }
                _client.ProjectUserRepository.Save();

                _client.ProjectRepository.Delete(existingProject.Id);
                _client.ProjectRepository.Save();

                return new JsonResult(new { success = true, message = "Project and all related data deleted successfully." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "An error occurred while deleting the project." });
            }
        }
    }
}