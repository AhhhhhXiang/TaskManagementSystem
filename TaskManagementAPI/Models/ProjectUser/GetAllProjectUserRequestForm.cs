using System.ComponentModel.DataAnnotations;

namespace TaskManagementAPI.Models.ProjectUser
{
    public class GetAllProjectUserRequestForm
    {
        public Guid? ProjectId { get; set; }
        public Guid? UserId { get; set; }
        public List<string>? modules { get; set; }
    }
}
