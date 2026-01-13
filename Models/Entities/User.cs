using System.ComponentModel.DataAnnotations;

namespace FPSample.Models.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string FirstName { get; set; }

        public string? MiddleName { get; set; } // Optional

        public string? Suffix { get; set; } // Optional

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Sex { get; set; }

        [Required]
        public string CivilStatus { get; set; }

        [Required]
        public string Religion { get; set; }

        [Required]
        public string HouseNoStreet { get; set; }

        [Required]
        public string Barangay { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string Province { get; set; }

        [Required]
        public int StayYears { get; set; }

        [Required]
        public int StayMonths { get; set; }

        [Required]
        public string ContactNo { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool IsVoter { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public bool IsActive { get; set; } = true;
    }
}