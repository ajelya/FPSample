using System.ComponentModel.DataAnnotations;

namespace FPSample.Models.Entities
{
    public class Status
    {
        [Key]
        public int StatusId { get; set; }
        public string StatusName { get; set; }
    }
}
