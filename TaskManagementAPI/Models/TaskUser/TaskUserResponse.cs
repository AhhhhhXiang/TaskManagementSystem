namespace TaskManagementAPI.Models.TaskUser
{
    public class TaskUserResponse
    {
        public Int64 Id { get; set; }
        public Guid? TaskId { get; set; }
        public Guid? UserId { get; set; }
    }
}
