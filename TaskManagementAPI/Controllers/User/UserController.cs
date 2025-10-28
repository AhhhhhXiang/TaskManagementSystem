using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Data.Migrations.Models;
using TaskManagementAPI.Models.User;

namespace TaskManagementAPI.Controllers.User
{
    [Authorize(Roles = constRoles.Administrator)]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<JsonResult> GetAll()
        {
            var users = _userManager.Users.ToList();
            var registerUsers = new List<UserResponse>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(constRoles.RegisterUser))
                {
                    registerUsers.Add(new UserResponse
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        Roles = roles
                    });
                }
            }

            var response = new GetAllUserResponse
            {
                users = registerUsers
            };

            return new JsonResult(response);
        }

        [HttpGet("{id}")]
        public async Task<JsonResult> Get(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return new JsonResult(new { message = "User not found" }) { StatusCode = StatusCodes.Status404NotFound };

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(constRoles.RegisterUser))
                return new JsonResult(new { message = "User not found" }) { StatusCode = StatusCodes.Status404NotFound };

            var result = new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles
            };

            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] CreateUserRequestForm form)
        {
            if (form == null)
                return new JsonResult(new { message = "Invalid data." }) { StatusCode = StatusCodes.Status400BadRequest };

            var user = new IdentityUser
            {
                UserName = form.UserName,
                Email = form.Email
            };

            var result = await _userManager.CreateAsync(user, form.Password);
            if (!result.Succeeded)
                return new JsonResult(result.Errors) { StatusCode = StatusCodes.Status400BadRequest };

            if (Enum.IsDefined(typeof(UserRoles), form.Role))
                await _userManager.AddToRoleAsync(user, form.Role.ToString());

            var response = new CreateUserResponse
            {
                UserId = Guid.Parse(user.Id),
                UserName = user.UserName,
                Email = user.Email,
                Password = form.Password,
                Role = form.Role
            };

            return new JsonResult(response) { StatusCode = StatusCodes.Status201Created };
        }

        [HttpPatch("{id}")]
        public async Task<JsonResult> Update(string id, [FromBody] UpdateUserRequestForm model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return new JsonResult(new { message = "User not found" }) { StatusCode = StatusCodes.Status404NotFound };

            bool isModified = false;

            if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName != user.UserName)
            {
                user.UserName = model.UserName;
                isModified = true;
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && model.Email != user.Email)
            {
                user.Email = model.Email;
                isModified = true;
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passResult.Succeeded)
                    return new JsonResult(passResult.Errors) { StatusCode = StatusCodes.Status400BadRequest };

                isModified = true;
            }

            if (model.Role.HasValue)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var newRole = model.Role.Value.ToString();

                if (!currentRoles.Contains(newRole))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, newRole);
                    isModified = true;
                }
            }

            if (isModified)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return new JsonResult(updateResult.Errors) { StatusCode = StatusCodes.Status400BadRequest };
            }

            var response = new UpdateUserResponse
            {
                UserId = Guid.Parse(user.Id),
                UserName = user.UserName,
                Email = user.Email,
                Password = model.Password,
                Role = model.Role
            };

            return new JsonResult(response);
        }

        [HttpDelete("{id}")]
        public async Task<JsonResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return new JsonResult(new { message = "User not found" }) { StatusCode = StatusCodes.Status404NotFound };

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return new JsonResult(result.Errors) { StatusCode = StatusCodes.Status400BadRequest };

            return new JsonResult(new { message = "User deleted successfully" }) { StatusCode = StatusCodes.Status204NoContent };
        }
    }
}
