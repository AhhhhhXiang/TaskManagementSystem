using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Data.Migrations.Models
{
    public class TaskAttachmentReturnModel
    {
        public Int64 Id { get; set; }
        public Guid TaskId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
