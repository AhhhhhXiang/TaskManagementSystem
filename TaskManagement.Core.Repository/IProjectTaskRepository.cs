using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository
{
    public interface IProjectTaskRepository
    {
        IEnumerable<ProjectTask> GetAll();
        ProjectTask? GetById(Guid taskId);
        void Add(ProjectTask projectTask);
        void Update(ProjectTask projectTask);
        void Delete(Guid? taskId);
        void Save();
    }
}
