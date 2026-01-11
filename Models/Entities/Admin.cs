using System.ComponentModel.DataAnnotations;

namespace FPSample.Models.Entities
{
    public class Admin
    {
        [Key]
        public int AdminId { get; set; }
        public string Username { get; set; }
        public string AdminPassword { get; set; }
    }
}
