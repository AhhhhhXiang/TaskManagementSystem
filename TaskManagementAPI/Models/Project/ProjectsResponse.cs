using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TaskManagement.Data.Migrations.Models;

namespace TaskManagementAPI.Models.Project
{
    public class ProjectsResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string? Description { get; set; }
        public List<ProjectTaskReturnModel>? projectTasks { get; set; }
        public List<ProjectUserReturnModel>? projectUsers { get; set; }
    }
}