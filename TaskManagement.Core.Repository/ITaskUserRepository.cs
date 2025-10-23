using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Data.Migrations.Models;

namespace TaskManagement.Core.Repository
{
    public interface ITaskUserRepository
    {
        IEnumerable<TaskUser> GetAll();
        TaskUser? GetById(Int64 taskUserId);
        void Add(TaskUser taskUser);
        void Update(TaskUser taskUser);
        void Delete(Int64? taskUserId);
        void Save();
    }
}
