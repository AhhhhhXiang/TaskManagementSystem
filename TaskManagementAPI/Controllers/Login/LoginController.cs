using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using TaskManagementAPI.Models.Login;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;

        public LoginController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequestForm form)
        {
            Console.WriteLine($"=== Login Attempt ===");
            Console.WriteLine($"Username: {form.Username}");

            if (string.IsNullOrEmpty(form.Username) || string.IsNullOrEmpty(form.Password))
            {
                return BadRequest(new { success = false, message = "Username and password are required." });
            }

            var user = await _userManager.FindByNameAsync(form.Username)
                       ?? await _userManager.FindByEmailAsync(form.Username);

            if (user == null)
            {
                Console.WriteLine("User not found");
                return Unauthorized(new { success = false, message = "Invalid credentials" });
            }

            Console.WriteLine($"User found: {user.UserName}, ID: {user.Id}");

            var result = await _signInManager.CheckPasswordSignInAsync(user, form.Password, false);

            if (!result.Succeeded)
            {
                Console.WriteLine("Password check failed");
                return Unauthorized(new { success = false, message = "Invalid credentials" });
            }

            var token = await GenerateJwtToken(user);

            Console.WriteLine("Login successful, token generated");

            return Ok(new
            {
                success = true,
                message = "Login successful!",
                token
            });
        }

        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            Console.WriteLine("=== Token Claims ===");
            foreach (var claim in claims)
            {
                Console.WriteLine($"{claim.Type}: {claim.Value}");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            Console.WriteLine($"Token generated (first 50 chars): {tokenString.Substring(0, Math.Min(50, tokenString.Length))}...");

            return tokenString;
        }
    }
}