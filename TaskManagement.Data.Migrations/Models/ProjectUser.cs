using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Data.Migrations.Models
{
    public class ProjectUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 Id { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public Guid UserId { get; set; }
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
