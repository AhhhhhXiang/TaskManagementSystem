using TaskManagement.Data.Migrations.Data;
using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository
{
    public class ProjectTaskRepository : IProjectTaskRepository
    {
        private readonly AppDbContext _context;

        public ProjectTaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<ProjectTask> GetAll()
        {
            return _context.ProjectTasks
                .Where(t => t.status == 1)
                .ToList();
        }

        public ProjectTask? GetById(Guid taskId)
        {
            if (taskId != null)
            {
                var x = _context.ProjectTasks
                .Where(b => b.Id == taskId)
                .FirstOrDefault();

                return x;
            }
            else
            {
                return null;
            }
        }

        public void Add(ProjectTask projectTask)
        {
            _context.ProjectTasks.Add(projectTask);
        }

        public void Update(ProjectTask projectTask)
        {
            _context.ProjectTasks.Update(projectTask);
        }

        public void Delete(Guid? taskId)
        {
            ProjectTask? projectTask = _context.ProjectTasks.Find(taskId);
            if (projectTask != null)
            {
                _context.ProjectTasks.Remove(projectTask);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
