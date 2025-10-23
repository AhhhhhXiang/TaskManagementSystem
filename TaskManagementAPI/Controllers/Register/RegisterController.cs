using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Data.Migrations.Models;
using TaskManagementAPI.Models.Register;

[Route("api/[controller]")]
[ApiController]
public class RegisterController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public RegisterController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpPost]
    public async Task<JsonResult> Register([FromBody] RegisterRequestForm form)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        var existingUser = await _userManager.FindByEmailAsync(form.Email);
        if (existingUser != null)
            return new JsonResult(new { success = false, message = "Email already exists." });

        var user = new IdentityUser
        {
            UserName = form.Username,
            Email = form.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, form.Password);
        if (!result.Succeeded)
        {
            return new JsonResult(new
            {
                success = false,
                message = "User creation failed.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        string defaultRole = UserRoles.RegisterUser.ToString();
        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(defaultRole));
        }
        await _userManager.AddToRoleAsync(user, defaultRole);

        return new JsonResult(new { success = true, message = "User registered successfully!", role = defaultRole });
    }
}
