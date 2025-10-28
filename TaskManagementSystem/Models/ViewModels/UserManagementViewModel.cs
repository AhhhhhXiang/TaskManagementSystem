using TaskManagementAPI.Models.User;

namespace TaskManagementSystem.Models.ViewModels
{
    public class UserManagementViewModel
    {
        public List<UserResponse> Users { get; set; } = new List<UserResponse>();
    }
}