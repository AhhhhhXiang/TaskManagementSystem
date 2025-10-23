namespace TaskManagement.Core.Repository
{
    public interface ITaskManagementClient
    {
        IProjectRepository ProjectRepository { get; }
        IProjectTaskRepository ProjectTaskRepository { get; }
        IProjectUserRepository ProjectUserRepository { get; }
        ITaskAttachmentRepository TaskAttachmentRepository { get; }
        ITaskUserRepository TaskUserRepository { get; }
    }
}
