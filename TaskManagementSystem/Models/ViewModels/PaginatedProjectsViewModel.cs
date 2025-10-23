using TaskManagementAPI.Models.Project;

namespace TaskManagementSystem.Models.ViewModels
{
    public class PaginatedProjectsViewModel
    {
        public List<ProjectsResponse> Projects { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
