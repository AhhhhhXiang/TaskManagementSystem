namespace TaskManagement.Core.Repository
{
    public class TaskManagementClient : ITaskManagementClient
    {
        public IProjectRepository ProjectRepository { get; }
        public IProjectTaskRepository ProjectTaskRepository { get; }
        public IProjectUserRepository ProjectUserRepository { get; }
        public ITaskAttachmentRepository TaskAttachmentRepository { get; }
        public ITaskUserRepository TaskUserRepository { get; }

        public TaskManagementClient(
            IProjectRepository projectRepository,
            IProjectTaskRepository projectTaskRepository,
            IProjectUserRepository projectUserRepository,
            ITaskAttachmentRepository taskAttachmentRepository,
            ITaskUserRepository taskUserRepository)
        {
            ProjectRepository = projectRepository;
            ProjectTaskRepository = projectTaskRepository;
            ProjectUserRepository = projectUserRepository;
            TaskAttachmentRepository = taskAttachmentRepository;
            TaskUserRepository = taskUserRepository;
        }
    }
}
