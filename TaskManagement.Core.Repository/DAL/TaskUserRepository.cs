using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Data.Migrations.Data;
using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository.DAL
{
    public class TaskUserRepository : ITaskUserRepository
    {
        private readonly AppDbContext _context;

        public TaskUserRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<TaskUser> GetAll()
        {
            return _context.TaskUsers
                .ToList();
        }

        public TaskUser? GetById(Int64 taskUserId)
        {
            if (taskUserId != null)
            {
                var x = _context.TaskUsers
                .Where(b => b.Id == taskUserId)
                .FirstOrDefault();

                return x;
            }
            else
            {
                return null;
            }
        }

        public void Add(TaskUser taskUser)
        {
            _context.TaskUsers.Add(taskUser);
        }

        public void Update(TaskUser taskUser)
        {
            _context.TaskUsers.Update(taskUser);
        }

        public void Delete(Int64? taskUserId)
        {
            TaskUser? taskUser = _context.TaskUsers.Find(taskUserId);
            if (taskUser != null)
            {
                _context.TaskUsers.Remove(taskUser);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
