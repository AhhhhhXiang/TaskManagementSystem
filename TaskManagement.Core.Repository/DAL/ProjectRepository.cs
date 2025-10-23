using System.Drawing;
using TaskManagement.Data.Migrations.Data;
using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _context;

        public ProjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Project> GetAll()
        {
            return _context.Projects
                .Where(p => p.status == 1)
                .OrderByDescending(p => p.CreatedDateTime)
                .ToList();
        }

        public Project? GetById(Guid projectId)
        {
            return _context.Projects.FirstOrDefault(p => p.Id == projectId);
        }

        public void Add(Project project)
        {
            _context.Projects.Add(project);
        }

        public void Update(Project project)
        {
            _context.Projects.Update(project);
        }

        public void Delete(Guid? projectId)
        {
            Project? projectInfo = _context.Projects
                .Where(colour => colour.Id == projectId)
                .FirstOrDefault();

            if (projectInfo != null)
            {
                _context.Projects.Remove(projectInfo);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
