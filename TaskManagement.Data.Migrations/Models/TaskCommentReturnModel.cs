using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Data.Migrations.Models
{
    public class TaskCommentReturnModel
    {
        public Int64 Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
