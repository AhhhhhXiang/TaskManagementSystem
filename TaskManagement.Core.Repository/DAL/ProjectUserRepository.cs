using System.Threading.Tasks;
using TaskManagement.Data.Migrations.Data;
using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository
{
    public class ProjectUserRepository : IProjectUserRepository
    {
        private readonly AppDbContext _context;

        public ProjectUserRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<ProjectUser> GetAll()
        {
            return _context.ProjectUsers
                .ToList();
        }

        public ProjectUser? GetById(Int64 projectUserId)
        {
            if (projectUserId != null)
            {
                var x = _context.ProjectUsers
                .Where(b => b.Id == projectUserId)
                .FirstOrDefault();

                return x;
            }
            else
            {
                return null;
            }
        }

        public void Add(ProjectUser projectUser)
        {
            _context.ProjectUsers.Add(projectUser);
        }

        public void Update(ProjectUser projectUser)
        {
            _context.ProjectUsers.Update(projectUser);
        }

        public void Delete(Int64? projectUserId)
        {
            ProjectUser? projectUser = _context.ProjectUsers.Find(projectUserId);
            if (projectUser != null)
            {
                _context.ProjectUsers.Remove(projectUser);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
