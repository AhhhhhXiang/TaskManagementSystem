using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository
{
    public interface ITaskCommentRepository
    {
        IEnumerable<TaskComment> GetAll();
        TaskComment? GetById(Int64 taskCommentId);
        void Add(TaskComment taskComment);
        void Update(TaskComment taskComment);
        void Delete(Int64? taskCommentId);
        void Save();
    }
}
