using TaskManagement.Data.Migrations.Models;
using TaskManagementAPI.Models.Project;

namespace TaskManagementSystem.Models.ViewModels
{
    public class ProjectDetailsViewModel
    {
        public ProjectsResponse project { get; set; }
        public List<UserReturnModel> users { get; set; }
        public Guid currentUserId { get; set; }
    }
}
