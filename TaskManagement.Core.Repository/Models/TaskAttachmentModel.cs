using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Core.Repository.Models
{
    public class TaskAttachmentModel
    {
        public Int64 Id { get; set; }
        public Guid? TaskId { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public Byte status { get; set; }
        public string? Remarks { get; set; }
    }
}
