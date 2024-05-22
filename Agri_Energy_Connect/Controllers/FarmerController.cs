using Agri_Energy_Connect.Areas.Identity.Data;
using Agri_Energy_Connect.Data;
using Agri_Energy_Connect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Agri_Energy_Connect.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuthDbContext _context;
        public FarmerController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AuthDbContext context)
        {
            this._logger = logger;
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._context = context;
        }

        public async Task<IActionResult> CreateProduct()
        {
            //get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                //get the user's role
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault();

                //store the user's ID and role in ViewData
                ViewData["UserId"] = user.Id;
                ViewData["UserFirstName"] = user.FirstName;
                ViewData["UserRole"] = userRole;
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductModel product)
        {
            ModelState.Remove(nameof(ProductModel.UserId));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogError(error.ErrorMessage);
                }
                return View(product);
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    //assign the user's ID to the product
                    product.UserId = user.Id;

                    //add the product to the database
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(ViewProducts));
                }
            }
            return View(product);
        }

        public async Task<IActionResult> ViewProducts()
        {
            //get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                //get the user's role
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault();

                //store the user's ID and role in ViewData
                ViewData["UserId"] = user.Id;
                ViewData["UserFirstName"] = user.FirstName;
                ViewData["UserRole"] = userRole;
            }
            return View();
        }
    }
}
