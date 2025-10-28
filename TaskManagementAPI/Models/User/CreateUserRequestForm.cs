using TaskManagement.Data.Migrations.Models;

namespace TaskManagementAPI.Models.User
{
    public class CreateUserRequestForm
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public UserRoles Role { get; set; }
    }
}
