using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using TaskManagementAPI.Models.Attachment;
using TaskManagement.Core.Repository;
using Microsoft.AspNetCore.Authorization;

namespace TaskManagementAPI.Controllers.Attachments
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttachmentController : ControllerBase
    {
        private readonly ITaskManagementClient _client;
        private readonly IConfiguration _configuration;

        public AttachmentController(ITaskManagementClient client, IConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
        }

        [Authorize]
        [HttpPost("Upload")]
        public async Task<JsonResult> Upload([FromForm] UploadAttachmentRequestForm form)
        {
            if (form.file == null || form.file.Length == 0)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "No file uploaded."
                });
            }

            string baseFolder = _configuration["AttachmentPath:temp"];
            string folderName = _configuration["AttachmentPath:attachmentFolder"];

            string todayDate = DateTime.UtcNow.ToString("yyyyMMdd");
            string folderPath = Path.Combine(baseFolder, folderName, todayDate);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string extension = Path.GetExtension(form.file.FileName);
            string fileName = $"{todayDate}_{Guid.NewGuid()}{extension}";
            string fullPath = Path.Combine(folderPath, fileName);

            using (var stream = System.IO.File.Create(fullPath))
            {
                await form.file.CopyToAsync(stream);
            }

            string relativePath = $"{todayDate}\\{fileName}";

            return new JsonResult(new
            {
                success = true,
                message = "File uploaded successfully.",
                fileName = form.file.FileName,
                path = relativePath
            });
        }

        [HttpGet("GetAttachment")]
        public IActionResult GetAttachment([FromQuery] long attachmentId)
        {
            var attachment = _client.TaskAttachmentRepository.GetById(attachmentId);
            if (attachment == null)
                return NotFound("Attachment not found.");

            try
            {
                var relativePath = attachment.FilePath.TrimStart('\\', '/');
                var basePath = _configuration.GetSection("AttachmentPath:get").Value;
                var folder = _configuration.GetSection("AttachmentPath:getAttachmentFolder").Value;
                var fullPath = Path.Combine(basePath, folder, relativePath);

                if (!System.IO.File.Exists(fullPath))
                    return NotFound("File not found on server.");

                var fileBytes = System.IO.File.ReadAllBytes(fullPath);
                var contentType = GetContentType(fullPath);
                var fileName = Path.GetFileName(fullPath);

                var contentDisposition = contentType.StartsWith("image/") || contentType == "application/pdf"
                    ? "inline"
                    : "attachment";

                Response.Headers["Content-Disposition"] = $"{contentDisposition}; filename=\"{fileName}\"";

                return new FileContentResult(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reading file.", detail = ex.Message });
            }
        }

        private string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".doc" or ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" or ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}
