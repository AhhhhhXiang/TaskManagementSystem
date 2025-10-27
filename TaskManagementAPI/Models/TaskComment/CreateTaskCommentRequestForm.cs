using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaskManagementAPI.Models.TaskComment
{
    public class CreateTaskCommentRequestForm
    {
        public string? TaskId { get; set; }
        public string? UserId { get; set; }
        public string Comment { get; set; }
    }
}
