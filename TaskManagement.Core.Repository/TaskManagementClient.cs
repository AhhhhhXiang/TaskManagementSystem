namespace TaskManagement.Core.Repository
{
    public class TaskManagementClient : ITaskManagementClient
    {
        public IProjectRepository ProjectRepository { get; }
        public IProjectTaskRepository ProjectTaskRepository { get; }
        public IProjectUserRepository ProjectUserRepository { get; }
        public ITaskAttachmentRepository TaskAttachmentRepository { get; }
        public ITaskUserRepository TaskUserRepository { get; }
        public ITaskCommentRepository TaskCommentRepository { get; }

        public TaskManagementClient(
            IProjectRepository projectRepository,
            IProjectTaskRepository projectTaskRepository,
            IProjectUserRepository projectUserRepository,
            ITaskAttachmentRepository taskAttachmentRepository,
            ITaskUserRepository taskUserRepository,
            ITaskCommentRepository taskCommentRepository)
        {
            ProjectRepository = projectRepository;
            ProjectTaskRepository = projectTaskRepository;
            ProjectUserRepository = projectUserRepository;
            TaskAttachmentRepository = taskAttachmentRepository;
            TaskUserRepository = taskUserRepository;
            TaskCommentRepository = taskCommentRepository;
        }
    }
}
