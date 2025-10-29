using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Data.Migrations.Models
{
    public class ProjectTaskReturnModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskStatus ProgressStatus { get; set; }
        public PriorityStatus PriorityStatus { get; set; }
        public List<UserReturnModel>? taskUsers { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public List<TaskAttachmentReturnModel>? taskAttachments { get; set; }
        public List<TaskCommentReturnModel>? taskComments { get; set; }
        public int TotalTaskCount { get; set; }
        public int TaskPage { get; set; }
        public int TaskPageSize { get; set; }
    }
}