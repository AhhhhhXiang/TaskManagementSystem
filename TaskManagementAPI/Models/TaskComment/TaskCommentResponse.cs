namespace TaskManagementAPI.Models.TaskComment
{
    public class TaskCommentResponse
    {
        public Int64 Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
