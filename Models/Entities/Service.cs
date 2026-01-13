using System.ComponentModel.DataAnnotations;

namespace FPSample.Models.Entities
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } // e.g., Barangay Clearance
        public bool IsEnabled { get; set; } = true;
        public string? Description { get; set; }
        public virtual ICollection<ServicePurpose> ServicePurposes { get; set; } = new List<ServicePurpose>();
    }
}
