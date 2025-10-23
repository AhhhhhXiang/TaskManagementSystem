namespace TaskManagementAPI.Models.TaskAttachment
{
    public class CreateTaskAttachmentRequestForm
    {
        public Guid TaskId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
