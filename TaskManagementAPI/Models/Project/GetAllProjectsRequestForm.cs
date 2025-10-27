namespace TaskManagementAPI.Models.Project
{
    public class GetAllProjectsRequestForm
    {
        public int page { get; set; }
        public int pageSize { get; set; }
        public string? projectName { get; set; }
        public string? memberName { get; set; }
        public string? priority { get; set; }
        public List<string>? modules { get; set; }
    }
}
