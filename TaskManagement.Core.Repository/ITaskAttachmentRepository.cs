using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository
{
    public interface ITaskAttachmentRepository
    {
        IEnumerable<TaskAttachment> GetAll();
        TaskAttachment? GetById(Int64 taskId);
        void Add(TaskAttachment taskAttachment);
        void Update(TaskAttachment taskAttachment);
        void Delete(Int64? taskId);
        void Save();
    }
}
