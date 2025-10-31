namespace TaskManagementAPI.Models.Project
{
    public class GetProjectRequestForm
    {
        public List<string>? modules { get; set; }
        public string? taskName { get; set; }
        public DateTime? taskStartDate { get; set; }
        public DateTime? taskEndDate { get; set; }
        public string? taskPriority { get; set; }
        public string? taskUserId { get; set; }
        public string? taskSortBy { get; set; }
        public string? taskSortOrder { get; set; }
        public int taskPage { get; set; } = 1;
        public int taskPageSize { get; set; } = 10;
    }
}
