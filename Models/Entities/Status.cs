using System.ComponentModel.DataAnnotations;

namespace FPSample.Models.Entities
{
    public class Status
    {
        [Key]
        public int Id { get; set; }
        public string StatusName { get; set; }
    }
}
