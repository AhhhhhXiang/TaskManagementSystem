using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Data.Migrations.Data;
using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository.DAL
{
    public class TaskCommentRepository : ITaskCommentRepository
    {

        private readonly AppDbContext _context;

        public TaskCommentRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<TaskComment> GetAll()
        {
            return _context.TaskComments
                .ToList();
        }

        public TaskComment? GetById(Int64 taskCommentId)
        {
            if (taskCommentId != null)
            {
                var x = _context.TaskComments
                .Where(b => b.Id == taskCommentId)
                .FirstOrDefault();

                return x;
            }
            else
            {
                return null;
            }
        }

        public void Add(TaskComment taskComment)
        {
            _context.TaskComments.Add(taskComment);
        }

        public void Update(TaskComment taskComment)
        {
            _context.TaskComments.Update(taskComment);
        }

        public void Delete(Int64? taskCommentId)
        {
            TaskComment? taskComment = _context.TaskComments.Find(taskCommentId);
            if (taskComment != null)
            {
                _context.TaskComments.Remove(taskComment);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
