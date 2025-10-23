namespace TaskManagementAPI.Models.ProjectTask
{
    public class CreateProjectTaskRequestForm
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskManagement.Data.Migrations.Models.TaskStatus ProgressStatus { get; set; }
    }
}
