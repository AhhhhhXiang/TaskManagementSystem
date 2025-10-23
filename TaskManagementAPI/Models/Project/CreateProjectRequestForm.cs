using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagementAPI.Models.Project
{
    public class CreateProjectRequestForm
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Remarks { get; set; }
    }
}
