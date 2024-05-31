using Agri_Energy_Connect.Areas.Identity.Data;
using Agri_Energy_Connect.Data;
using Agri_Energy_Connect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agri_Energy_Connect.Controllers
{
    //lock controller and actions to Farmer role users only
    [Authorize(Roles = "Farmer")]
    public class FarmerController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuthDbContext _context;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userManager"></param>
        /// <param name="signInManager"></param>
        /// <param name="context"></param>
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

        /// <summary>
        /// Create Product Inital Event
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Cretae Product Event Handler
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductModel product)
        {
            //temporarily remove the UserId from the model state
            ModelState.Remove(nameof(ProductModel.UserId));

            //check if the model state is valid 
            if (!ModelState.IsValid)
            {
                //if there is no model state get a list of errors
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                //log each error
                foreach (var error in errors)
                {
                    _logger.LogError(error.ErrorMessage);
                }
                return View(product);
            }

            //if there is a valid model
            if (ModelState.IsValid)
            {
                //get the current user
                var user = await _userManager.GetUserAsync(User);
                //if the user exists
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


        /// <summary>
        /// View Products Event Handler
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> ViewProducts()
        {
            //get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                //get the user's role
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault();

                //store the user's, first name, ID and role in ViewData
                ViewData["UserId"] = user.Id;
                ViewData["UserFirstName"] = user.FirstName;
                ViewData["UserRole"] = userRole;

                //get the list of products that related to the current user and pass to a list
                var products = await _context.Products
                .Where(p => p.UserId == user.Id)
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category,
                    ProductionDate = p.ProductionDate
                })
                .ToListAsync();

                return View(products);
            } else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Delete Product Event Handler
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            //get current user
            var user = await _userManager.GetUserAsync(User);
            //check if the user exists
            if (user == null)
            {
                return NotFound();//error out
            }

            //find the selected product in the database
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            //check if the product exsists
            if (product == null)
            {
                return NotFound();//error out
            }

            //delete the product
            _context.Products.Remove(product);
            //update the database
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ViewProducts));
        }
    }
}
//---------------....oooOO0_END_OF_FILE_0OOooo....---------------\\