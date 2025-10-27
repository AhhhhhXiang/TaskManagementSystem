using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Data.Migrations.Models
{
    public enum UserRoles
    {
        [Display(Name = constRoles.Administrator)]
        Administrator = 1,

        [Display(Name = constRoles.RegisterUser)]
        RegisterUser = 2,
    }

    public static class constRoles
    {
        public const string Administrator = "Administrator";
        public const string RegisterUser = "RegisterUser";
    }

    public enum TaskStatus : byte
    {
        [Display(Name = constTaskStatus.ToDo)]
        ToDo = 1,

        [Display(Name = constTaskStatus.InProgress)]
        InProgress = 2,

        [Display(Name = constTaskStatus.Done)]
        Done = 3,

        [Display(Name = constTaskStatus.ToBeReviewed)]
        ToBeReviewed = 4,

        [Display(Name = constTaskStatus.ToBeCorrected)]
        ToBeCorrected = 5
    }

    public static class constTaskStatus
    {
        public const string ToDo = "To Do";
        public const string InProgress = "In Progress";
        public const string Done = "Done";
        public const string ToBeReviewed = "To Be Reviewed";
        public const string ToBeCorrected = "To Be Corrected";
    }

    public enum PriorityStatus : byte
    {
        [Display(Name = constPriorityStatus.Low)]
        Low = 1,

        [Display(Name = constPriorityStatus.Medium)]
        Medium = 2,

        [Display(Name = constPriorityStatus.High)]
        High = 3
    }

    public static class constPriorityStatus
    {
        public const string Low = "Low";
        public const string Medium = "Medium";
        public const string High = "High";
    }
}
