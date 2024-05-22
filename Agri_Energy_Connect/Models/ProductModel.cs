using System;
using System.ComponentModel.DataAnnotations;

namespace Agri_Energy_Connect.Models
{
    public class ProductModel
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; }

        [DataType(DataType.Date)]
        public DateTime ProductionDate { get; set; }
    }
}