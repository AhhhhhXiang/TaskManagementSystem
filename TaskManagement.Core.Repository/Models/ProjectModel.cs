namespace TaskManagement.Core.Repository.Models
{
    public class ProjectModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public Byte status { get; set; }
        public string? Remarks { get; set; }
    }
}
