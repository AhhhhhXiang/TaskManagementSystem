using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagement.Core.Repository;
using TaskManagement.Data.Migrations.Models;
using TaskManagementAPI.Models.ProjectUser;

namespace TaskManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectUserController : ControllerBase
    {
        private readonly ITaskManagementClient _client;
        private readonly UserManager<IdentityUser> _userManager;

        public ProjectUserController(ITaskManagementClient client, UserManager<IdentityUser> userManager)
        {
            _client = client;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetAll([FromQuery] GetAllProjectUserRequestForm form)
        {
            if (!form.ProjectId.HasValue && !form.UserId.HasValue)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Either ProjectId or UserId must be provided."
                });
            }

            var query = _client.ProjectUserRepository.GetAll().AsQueryable();

            if (form.ProjectId.HasValue)
            {
                query = query.Where(pu => pu.ProjectId == form.ProjectId.Value);
            }
            else if (form.UserId.HasValue)
            {
                query = query.Where(pu => pu.UserId == form.UserId.Value);
            }

            var projectUsers = query.ToList();

            var responseList = new List<ProjectUsersResponse>();
            foreach (var pu in projectUsers)
            {
                responseList.Add(new ProjectUsersResponse
                {
                    Id = pu.Id,
                    ProjectId = pu.ProjectId,
                    UserId = pu.UserId
                });
            }

            return new JsonResult(new GetAllProjectUsersResponse
            {
                projectUsers = responseList
            });
        }


        [Authorize]
        [HttpPost]
        public async Task<JsonResult> Create([FromBody] CreateProjectUserRequestForm form)
        {
            if (string.IsNullOrEmpty(form.ProjectId) || string.IsNullOrEmpty(form.UserId))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Both ProjectId and UserId are required."
                });
            }

            if (!Guid.TryParse(form.ProjectId, out var projectId))
            {
                return new JsonResult(new { success = false, message = "Invalid ProjectId format." });
            }

            if (!Guid.TryParse(form.UserId, out var userId))
            {
                return new JsonResult(new { success = false, message = "Invalid UserId format." });
            }

            var isExisting = _client.ProjectUserRepository
                .GetAll()
                .FirstOrDefault(pu => pu.ProjectId == projectId && pu.UserId == userId);

            if (isExisting != null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "This user is already assigned to the project."
                });
            }

            var projectUser = new TaskManagement.Data.Migrations.Models.ProjectUser
            {
                ProjectId = projectId,
                UserId = userId,
                CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedDateTime = DateTime.UtcNow,
                status = 1
            };

            _client.ProjectUserRepository.Add(projectUser);
            _client.ProjectUserRepository.Save();

            return new JsonResult(new CreateProjectUserResponse
            {
                projectUser = projectUser
            });
        }

        [Authorize]
        [HttpDelete("{projectUserId}")]
        public JsonResult Delete([FromRoute(Name = "projectUserId")] int projectUserId)
        {
            var projectUser = _client.ProjectUserRepository
                .GetAll()
                .FirstOrDefault(pu => pu.Id == projectUserId);

            if (projectUser == null)
                return new JsonResult(new { success = false, message = "ProjectUser not found." });

            _client.ProjectUserRepository.Delete(projectUser.Id);
            _client.ProjectUserRepository.Save();

            return new JsonResult(new { success = true, message = "ProjectUser deleted successfully." });
        }
    }
}
