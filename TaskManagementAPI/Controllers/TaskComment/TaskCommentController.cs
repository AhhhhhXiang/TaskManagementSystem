using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManagement.Core.Repository;
using TaskManagementAPI.Models.ProjectUser;
using TaskManagementAPI.Models.TaskAttachment;
using TaskManagementAPI.Models.TaskComment;

namespace TaskManagementAPI.Controllers.TaskComment
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskCommentController : ControllerBase
    {
        private readonly ITaskManagementClient _client;
        private readonly UserManager<IdentityUser> _userManager;

        public TaskCommentController(ITaskManagementClient client, UserManager<IdentityUser> userManager)
        {
            _client = client;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetAll([FromQuery] string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
                return new JsonResult(new { success = false, message = "TaskId is required." });

            if (!Guid.TryParse(taskId, out Guid id))
                return new JsonResult(new { success = false, message = "Invalid TaskId format." });

            // Retrieve all comments for the task
            var comments = _client.TaskCommentRepository
                .GetAll()
                .Where(comment => comment.TaskId == id)
                .ToList();

            var taskComments = new List<TaskCommentResponse>();

            foreach (var comment in comments)
            {
                var user = await _userManager.FindByIdAsync(comment.UserId.ToString());
                var username = user != null ? user.UserName : "Anonymous";

                taskComments.Add(new TaskCommentResponse
                {
                    Id = comment.Id,
                    TaskId = comment.TaskId,
                    UserId = comment.UserId,
                    Username = username,
                    Comment = comment.Comment,
                    CreatedDateTime = comment.CreatedDateTime
                });
            }

            return new JsonResult(new GetAllTaskCommentResponse
            {
                taskComments = taskComments
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> Create([FromBody] CreateTaskCommentRequestForm form)
        {
            if (string.IsNullOrEmpty(form.TaskId) || string.IsNullOrEmpty(form.UserId))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Both TaskId and UserId are required."
                });
            }

            if (!Guid.TryParse(form.TaskId, out var taskId))
            {
                return new JsonResult(new { success = false, message = "Invalid TaskId format." });
            }

            if (!Guid.TryParse(form.UserId, out var userId))
            {
                return new JsonResult(new { success = false, message = "Invalid UserId format." });
            }

            var taskComment = new TaskManagement.Data.Migrations.Models.TaskComment
            {
                TaskId = taskId,
                UserId = userId,
                Comment = form.Comment,
                CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedDateTime = DateTime.UtcNow.ToLocalTime(),
                status = 1
            };

            _client.TaskCommentRepository.Add(taskComment);
            _client.TaskCommentRepository.Save();

            return new JsonResult(new CreateTaskCommentResponse
            {
                taskComment = taskComment
            });
        }

        [Authorize]
        [HttpDelete("{taskCommentId}")]
        public JsonResult Delete([FromRoute(Name = "taskCommentId")] int taskCommentId)
        {
            var taskComment = _client.TaskCommentRepository
                .GetAll()
                .FirstOrDefault(pu => pu.Id == taskCommentId);

            if (taskComment == null)
                return new JsonResult(new { success = false, message = "Task Comment not found." });

            try
            {

                _client.TaskCommentRepository.Delete(taskComment.Id);
                _client.TaskCommentRepository.Save();

                return new JsonResult(new
                {
                    success = true,
                    message = "Task Comment deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "An error occurred while remove task comment."
                });
            }
        }
    }
}
