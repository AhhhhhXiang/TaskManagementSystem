namespace TaskManagementAPI.Models.ProjectTask
{
    public class GetAllProjecTasksRequestForm
    {
        public int page { get; set; }
        public int pageSize { get; set; }
        public string? projectId { get; set; }
        public string? taskName { get; set; }
        public string? memberName { get; set; }
        public string? priority { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
    }
}
