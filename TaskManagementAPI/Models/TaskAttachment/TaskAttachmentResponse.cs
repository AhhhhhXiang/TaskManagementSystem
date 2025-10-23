namespace TaskManagementAPI.Models.TaskAttachment
{
    public class TaskAttachmentResponse
    {
        public Int64 Id { get; set; }
        public Guid TaskId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
