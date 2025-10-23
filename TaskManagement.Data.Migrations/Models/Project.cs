using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagement.Data.Migrations.Models
{
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Column(TypeName = "varchar")]
        public string? Description { get; set; }
        [Required]
        [StringLength(50)]
        public string? CreatedBy { get; set; }
        [Required]
        public DateTime CreatedDateTime { get; set; }
        [StringLength(100)]
        [JsonPropertyName("UpdatedBy")]
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        [Required]
        [Column(TypeName = "tinyint")]
        public Byte status { get; set; }
        [StringLength(100)]
        public string? Remarks { get; set; }
    }
}
