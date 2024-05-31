using Agri_Energy_Connect.Areas.Identity.Data;

namespace Agri_Energy_Connect.Models
{

    /// <summary>
    /// Model for farmer data
    /// </summary>
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
//---------------....oooOO0_END_OF_FILE_0OOooo....---------------\\