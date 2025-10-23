using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagement.Core.Repository;
using TaskManagementAPI.Models.TaskAttachment;
using TaskManagement.Data.Migrations.Models;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Core.Repository.Models;

namespace TaskManagementAPI.Controllers.TaskAttachment
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskAttachmentController : ControllerBase
    {
        private readonly ITaskManagementClient _client;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public TaskAttachmentController(ITaskManagementClient client, UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _client = client;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<JsonResult> GetAll([FromQuery] string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
                return new JsonResult(new { success = false, message = "TaskId is required." });

            if (!Guid.TryParse(taskId, out Guid id))
                return new JsonResult(new { success = false, message = "Invalid TaskId format." });

            var task = _client.ProjectTaskRepository.GetAll().FirstOrDefault(t => t.Id == id);
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

            var attachments = _client.TaskAttachmentRepository
                .GetAll()
                .Where(attachment => attachment.TaskId == id)
                .Select(attachment => new TaskAttachmentResponse
                {
                    Id = attachment.Id,
                    TaskId = attachment.TaskId,
                    FileName = attachment.FileName,
                    FilePath = Url.ActionLink(action: "GetAttachment", controller: "Attachment") + "?attachmentId=" + attachment.Id
                })
                .ToList();

            return new JsonResult(new GetAllTaskAttachmentsResponse
            {
                taskAttachment = attachments
            });
        }

        [HttpGet("{attachmentId}")]
        public async Task<JsonResult> Get(string attachmentId)
        {
            if (!long.TryParse(attachmentId, out var id))
                return new JsonResult(new { success = false, message = "Invalid attachmentId format." });

            var attachment = _client.TaskAttachmentRepository.GetById(id);
            if (attachment == null)
                return new JsonResult(new { success = false, message = "Attachment not found." });

            var task = _client.ProjectTaskRepository.GetAll().FirstOrDefault(t => t.Id == attachment.TaskId);
            if (task == null)
                return new JsonResult(new { success = false, message = "Related task not found." });

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

            return new JsonResult(new TaskAttachmentResponse
            {
                Id = attachment.Id,
                TaskId = attachment.TaskId,
                FileName = attachment.FileName,
                FilePath = Url.ActionLink(action: "GetAttachment", controller: "Attachment") + "?attachmentId=" + attachment.Id
            });
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] CreateTaskAttachmentRequestForm form)
        {
            if (form.TaskId == Guid.Empty || string.IsNullOrEmpty(form.FileName) || string.IsNullOrEmpty(form.FilePath))
                return new JsonResult(new { success = false, message = "TaskId, FileName and FilePath are required." });

            var task = _client.ProjectTaskRepository.GetAll().FirstOrDefault(t => t.Id == form.TaskId);
            if (task == null)
                return new JsonResult(new { success = false, message = "Related task not found." });

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

            if (form.FilePath.Split("\\").Length != 2)
                return new JsonResult(new { success = false, message = "FilePath format invalid." });

            var fileDate = form.FilePath.Split("\\").First();
            var fileName = form.FilePath.Split("\\").Last();

            string sourcePath = Path.Combine(
                _configuration["AttachmentPath:temp"],
                _configuration["AttachmentPath:attachmentFolder"],
                fileDate
            );

            string targetPath = Path.Combine(
                _configuration["AttachmentPath:get"],
                _configuration["AttachmentPath:getAttachmentFolder"],
                fileDate
            );

            string sourceFile = Path.Combine(sourcePath, fileName);
            string destFile = Path.Combine(targetPath, fileName);

            Directory.CreateDirectory(targetPath);

            if (System.IO.File.Exists(sourceFile))
                System.IO.File.Move(sourceFile, destFile, true);

            var storedFilePath = $"{fileDate}/{fileName}";

            var newAttachment = new TaskManagement.Data.Migrations.Models.TaskAttachment
            {
                TaskId = form.TaskId,
                FileName = form.FileName,
                FilePath = storedFilePath,
                CreatedBy = userId,
                CreatedDateTime = DateTime.UtcNow,
                status = 1
            };

            _client.TaskAttachmentRepository.Add(newAttachment);
            _client.TaskAttachmentRepository.Save();

            return new JsonResult(new CreateTaskAttachmentResponse { taskAttachment = newAttachment });
        }

        [HttpPatch("{attachmentId}")]
        public async Task<JsonResult> Update(string attachmentId, [FromBody] UpdateTaskAttachmentRequestForm form)
        {
            if (!long.TryParse(attachmentId, out var id))
                return new JsonResult(new { success = false, message = "Invalid attachmentId format." });

            var attachment = _client.TaskAttachmentRepository.GetById(id);
            if (attachment == null)
                return new JsonResult(new { success = false, message = "Attachment not found." });

            var task = _client.ProjectTaskRepository.GetAll().FirstOrDefault(t => t.Id == attachment.TaskId);
            if (task == null)
                return new JsonResult(new { success = false, message = "Related task not found." });

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

            if (!string.IsNullOrEmpty(form.FilePath) && form.FilePath.Split("\\").Length == 2)
            {
                var fileDate = form.FilePath.Split("\\").First();
                var fileName = form.FilePath.Split("\\").Last();

                string sourcePath = Path.Combine(
                    _configuration["AttachmentPath:temp"],
                    _configuration["AttachmentPath:attachmentFolder"],
                    fileDate
                );

                string targetPath = Path.Combine(
                    _configuration["AttachmentPath:get"],
                    _configuration["AttachmentPath:getAttachmentFolder"],
                    fileDate
                );

                string sourceFile = Path.Combine(sourcePath, fileName);
                string destFile = Path.Combine(targetPath, fileName);

                Directory.CreateDirectory(targetPath);

                if (System.IO.File.Exists(sourceFile))
                    System.IO.File.Move(sourceFile, destFile, true);

                attachment.FilePath = $"{fileDate}/{fileName}";
            }

            attachment.FileName = form.FileName ?? attachment.FileName;
            attachment.UpdatedBy = userId;
            attachment.UpdatedDateTime = DateTime.UtcNow;

            _client.TaskAttachmentRepository.Update(attachment);
            _client.TaskAttachmentRepository.Save();

            return new JsonResult(new UpdateTaskAttachmentResponse
            {
                taskAttachment = new TaskAttachmentModel
                {
                    Id = attachment.Id,
                    TaskId = attachment.TaskId,
                    FileName = attachment.FileName,
                    FilePath = attachment.FilePath,
                    CreatedBy = attachment.CreatedBy,
                    CreatedDateTime = attachment.CreatedDateTime,
                    UpdatedBy = attachment.UpdatedBy,
                    UpdatedDateTime = attachment.UpdatedDateTime
                }
            });
        }

        [HttpDelete("{attachmentId}")]
        public async Task<JsonResult> Delete(string attachmentId)
        {
            if (!long.TryParse(attachmentId, out var id))
                return new JsonResult(new { success = false, message = "Invalid attachmentId format." });

            var attachment = _client.TaskAttachmentRepository.GetById(id);
            if (attachment == null)
                return new JsonResult(new { success = false, message = "Attachment not found." });

            var task = _client.ProjectTaskRepository.GetAll().FirstOrDefault(t => t.Id == attachment.TaskId);
            if (task == null)
                return new JsonResult(new { success = false, message = "Related task not found." });

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

                _client.TaskAttachmentRepository.Delete(attachment.Id);
                _client.TaskAttachmentRepository.Save();

                return new JsonResult(new
                {
                    success = true,
                    message = "Attachment deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "An error occurred while deleting the attachment."
                });
            }
        }
    }
}
