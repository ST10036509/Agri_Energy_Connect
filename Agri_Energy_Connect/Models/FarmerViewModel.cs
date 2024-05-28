using Agri_Energy_Connect.Areas.Identity.Data;

namespace Agri_Energy_Connect.Models
{
    public class FarmerViewModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public List<ProductModel> Products { get; set; }

        //filtering properties:
        public string SelectedCategory { get; set; }
        public DateTime? ProductionDate { get; set; }
    }
}
