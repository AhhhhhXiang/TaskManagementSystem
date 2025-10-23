using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository
{
    public interface IProjectUserRepository
    {
        IEnumerable<ProjectUser> GetAll();
        ProjectUser? GetById(Int64 projectUserId);
        void Add(ProjectUser projectUser);
        void Update(ProjectUser projectUser);
        void Delete(Int64? projectUserId);
        void Save();
    }
}
