using System.ComponentModel.DataAnnotations;

namespace FPSample.Models.Entities
{
    public class ServicePurpose
    {
        [Key]
        public int PurposeId { get; set; }
        public string PurposeName { get; set; }
        public int ServiceId { get; set; } 
        public bool IsEnabled { get; set; } = true;
        public virtual Service? Service { get; set; }

    }
}
