namespace TaskManagementAPI.Models.ProjectUser
{
    public class ProjectUsersResponse
    {
        public Int64 Id { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? UserId { get; set; }
    }
}
