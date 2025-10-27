using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Data.Migrations.Models
{
    public class ProjectTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [Required]
        public Guid ProjectId { get; set; }
        [Required]
        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string Title { get; set; }
        [StringLength(1000)]
        [Column(TypeName = "varchar")]
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskStatus ProgressStatus { get; set; }
        public PriorityStatus priorityStatus { get; set; }
#pragma warning disable 8618
        [Required]
        [StringLength(50)]
        public string? CreatedBy { get; set; }
#pragma warning restore 8618
        [Required]
        public DateTime CreatedDateTime { get; set; }
        [StringLength(50)]
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        [Required]
        [Column(TypeName = "tinyint")]
        public Byte status { get; set; }
        public string? Remarks { get; set; }
    }
}
