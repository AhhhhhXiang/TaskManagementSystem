namespace TaskManagementAPI.Models.Project
{
    public class GetAllProjectsResponse
    {
        public List<ProjectsResponse> projects { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
        public int totalCount { get; set; }
    }
}
