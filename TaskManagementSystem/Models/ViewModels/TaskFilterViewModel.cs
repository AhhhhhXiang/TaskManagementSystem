using TaskManagement.Data.Migrations.Models;
using TaskManagementAPI.Models.Project;

namespace TaskManagementSystem.Models.ViewModels
{
    public class TaskFilterViewModel
    {
        public string? TaskName { get; set; }
        public DateTime? TaskStartDate { get; set; }
        public DateTime? TaskEndDate { get; set; }
        public string? TaskPriority { get; set; }
        public string? TaskUserId { get; set; }
        public string? TaskSortBy { get; set; }
        public string? TaskSortOrder { get; set; }
        public int TaskPage { get; set; } = 1;
        public int TaskPageSize { get; set; } = 10;
        public int TotalTaskCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalTaskCount / TaskPageSize);
    }
}
