using System;
using System.ComponentModel.DataAnnotations;

namespace Agri_Energy_Connect.Models
{
    /// <summary>
    /// Model for product data
    /// </summary>
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
//---------------....oooOO0_END_OF_FILE_0OOooo....---------------\\