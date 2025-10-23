using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Data.Migrations.Data;
using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository.DAL
{
    public class TaskAttachmentRepository : ITaskAttachmentRepository
    {

        private readonly AppDbContext _context;

        public TaskAttachmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<TaskAttachment> GetAll()
        {
            return _context.TaskAttachments
                .ToList();
        }

        public TaskAttachment? GetById(Int64 taskAttachmentId)
        {
            if (taskAttachmentId != null)
            {
                var x = _context.TaskAttachments
                .Where(b => b.Id == taskAttachmentId)
                .FirstOrDefault();

                return x;
            }
            else
            {
                return null;
            }
        }

        public void Add(TaskAttachment taskAttachment)
        {
            _context.TaskAttachments.Add(taskAttachment);
        }

        public void Update(TaskAttachment taskAttachment)
        {
            _context.TaskAttachments.Update(taskAttachment);
        }

        public void Delete(Int64? taskAttachmentId)
        {
            TaskAttachment? taskAttachment = _context.TaskAttachments.Find(taskAttachmentId);
            if (taskAttachment != null)
            {
                _context.TaskAttachments.Remove(taskAttachment);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
