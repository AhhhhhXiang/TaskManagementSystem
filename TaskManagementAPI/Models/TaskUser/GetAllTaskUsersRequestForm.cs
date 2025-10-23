namespace TaskManagementAPI.Models.TaskUser
{
    public class GetAllTaskUsersRequestForm
    {
        public Guid? TaskId { get; set; }
        public Guid? UserId { get; set; }
    }
}
