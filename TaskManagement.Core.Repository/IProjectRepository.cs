using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository
{
    public interface IProjectRepository
    {
        IEnumerable<Project> GetAll();
        Project? GetById(Guid id);
        void Add(Project project);
        void Update(Project project);
        void Delete(Guid? projectId);
        void Save();
    }
}
