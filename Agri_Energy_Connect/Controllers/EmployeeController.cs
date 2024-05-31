using Agri_Energy_Connect.Areas.Identity.Data;
using Agri_Energy_Connect.Data;
using Agri_Energy_Connect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Agri_Energy_Connect.Controllers
{
    //lock controller and actions to Employee role users only
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuthDbContext _context;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userManager"></param>
        /// <param name="userStore"></param>
        /// <param name="signInManager"></param>
        /// <param name="emailSender"></param>
        /// <param name="roleManager"></param>
        /// <param name="context"></param>
        public EmployeeController(
            ILogger<EmployeeController> logger,
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            AuthDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// Create Farmer Inital Event
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> CreateFarmer(string returnUrl = null)
        {
            //get the current user
            var user = await _userManager.GetUserAsync(User);
            //check if the user exists
            if (user != null)
            {
                //get the users role
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault();

                //pass data
                ViewData["UserId"] = user.Id;
                ViewData["UserFirstName"] = user.FirstName;
                ViewData["UserRole"] = userRole;
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Create Farmer Event Handler
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateFarmer(InputModel Input, string returnUrl = null)
        {
            //establish a return url
            returnUrl ??= Url.Content("~/");

            //check if a model exists
            if (ModelState.IsValid)
            {
                //create an insatnce of a user
                var user = CreateUser();

                //add data to user instance
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.EmployeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);//assign current employee's id

                //cretae user and add to database
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                //if action is a success
                if (result.Succeeded)
                {
                    //log success
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    //email validatio is not done but code breaks when removed
                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                    //
                    //assign role of farmer to user
                    var farmerRole = "Farmer";
                    if (!await _roleManager.RoleExistsAsync(farmerRole))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(farmerRole));
                    }
                    //add user and their respective role to database
                    await _userManager.AddToRoleAsync(user, farmerRole);

                    //returm to page
                    return RedirectToAction("ViewFarmers", new { email = Input.Email, returnUrl = returnUrl });
                }

                //display errors if they occur
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(Input);
        }

        /// <summary>
        /// Create user an instance of a user
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private ApplicationUser CreateUser()
        {
            try
            {
                //get an instance of an ApplicationUser
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                //throw error
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    "override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        /// <summary>
        /// Check for email support
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            //check if email support is not eanbled
            if (!_userManager.SupportsUserEmail)
            {
                //throw error
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            //return the user store
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }

        /// <summary>
        /// Bind InputModel access
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Delete Farmer Event Handler
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFarmer(string id)
        {
            //get current user
            var user = await _userManager.GetUserAsync(User);
            //check if they exist
            if (user == null)
            {
                return NotFound();//error out
            }

            //find the selected farmer in the database
            var farmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.Id == id && f.EmployeeId == user.Id);

            //check if the famer exists
            if (farmer == null)
            {
                return NotFound();//error out
            }

            //fetch all producst related to the seklected farmer
            var products = _context.Products.Where(p => p.UserId == id);

            //remove all products related to the farmer from the database 
            _context.Products.RemoveRange(products);

            //remove the farmer from the database
            _context.Farmers.Remove(farmer);
            //update database
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ViewFarmers));
        }

        /// <summary>
        /// View Farmers Event Handler
        /// </summary>
        /// <param name="farmerId"></param>
        /// <param name="category"></param>
        /// <param name="productionDate"></param>
        /// <returns></returns>
        public async Task<IActionResult> ViewFarmers(string farmerId, string category, DateTime? productionDate)
        {
            //get the current user
            var user = await _userManager.GetUserAsync(User);
            //chec if the user exsists
            if (user == null)
            {
                return NotFound();//error out
            }

            //get the user's role
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            //get all farmers related to the user
            var farmers = await _context.Farmers
                .Where(f => f.EmployeeId == user.Id)
                .ToListAsync();

            //create instances of FarmerModel for all farmers and pass to a list
            var viewModel = farmers.Select(f => new FarmerViewModel
            {
                Id = f.Id,
                FirstName = f.FirstName,
                LastName = f.LastName,
                Email = f.Email,
                Products = _context.Products.Where(p => p.UserId == f.Id).ToList()
            }).ToList();

            //check if there are any farmers present
            if (!string.IsNullOrEmpty(farmerId))
            {
                //get the farmers modle 
                var selectedFarmer = viewModel.FirstOrDefault(f => f.Id == farmerId);

                //check if the farmer exists
                if (selectedFarmer != null)
                {
                    //get the farmers products and pass to a Queryable List
                    var products = selectedFarmer.Products.AsQueryable();

                    //chekc if there is a category filter
                    if (!string.IsNullOrEmpty(category))
                    {
                        //filter the products by the selected categroy
                        products = products.Where(p => p.Category == category);
                    }

                    //check if there is a date filter
                    if (productionDate.HasValue)
                    {
                        //filter the products by the selected date
                        products = products.Where(p => p.ProductionDate.Date == productionDate.Value.Date);
                    }

                    //pass Queryable list to ennumerable list and assign data to model
                    selectedFarmer.Products = products.ToList();
                    selectedFarmer.SelectedCategory = category;
                    selectedFarmer.ProductionDate = productionDate;
                }
            }

            ViewData["UserId"] = user.Id;
            ViewData["UserRole"] = userRole;

            return View(viewModel);
        }

        /// <summary>
        /// Filter Produts Event Handler
        /// </summary>
        /// <param name="farmerId"></param>
        /// <param name="category"></param>
        /// <param name="productionDate"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult FilterProducts(string farmerId, string category, DateTime? productionDate)
        {
            //get the list of products related ot the user =
            var productsQuery = _context.Products
                                        .Where(p => p.UserId == farmerId)
                                        .AsQueryable();

            //chekc if there is a category filter
            if (!string.IsNullOrEmpty(category))
            {
                //filter the products by the selected categroy
                productsQuery = productsQuery.Where(p => p.Category == category);
            }

            //chekc if there is a date filter
            if (productionDate.HasValue)
            {
                //filter the products by the selected date
                productsQuery = productsQuery.Where(p => p.ProductionDate.Date == productionDate.Value.Date);
            }

            //pass list to an ennumerable list
            var products = productsQuery.ToList();

            //Created a model of farmer data with updated products
            var model = new FarmerViewModel
            {
                Id = farmerId,
                Products = products
            };

            return PartialView("_ProductsPartial", model);
        }
    }

    /// <summary>
    /// Cladd for storing inputs
    /// </summary>
    public class InputModel
    {
        [DataType(DataType.Text)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
//---------------....oooOO0_END_OF_FILE_0OOooo....---------------\\