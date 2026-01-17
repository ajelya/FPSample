using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace FPSample.Models.Entities
{
    public class ServiceRequest
    {
        [Key]
        public int RequestId { get; set; }
        public int? UserId { get; set; }

        public int ServiceId { get; set; }
        public int PurposeId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GrossAnnualIncome { get; set; }
        public int StatusId { get; set; } = 0;
        public DateTime DateToClaim { get; set; }
        public TimeSpan TimeToClaim { get; set; }

        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? UploadPath { get; set; }

        [NotMapped]
        public IFormFile? ProfilePicture { get; set; }

     
        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; }

        public virtual ICollection<History> Histories { get; set; } = new List<History>();
    }
}