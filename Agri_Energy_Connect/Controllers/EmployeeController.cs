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

        [HttpGet]
        public async Task<IActionResult> CreateFarmer(string returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault();

                ViewData["UserId"] = user.Id;
                ViewData["UserFirstName"] = user.FirstName;
                ViewData["UserRole"] = userRole;
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateFarmer(InputModel Input, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.EmployeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    var farmerRole = "Farmer";
                    if (!await _roleManager.RoleExistsAsync(farmerRole))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(farmerRole));
                    }
                    await _userManager.AddToRoleAsync(user, farmerRole);

                    return RedirectToAction("ViewFarmers", new { email = Input.Email, returnUrl = returnUrl });
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(Input);
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    "override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFarmer(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var farmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.Id == id && f.EmployeeId == user.Id);

            if (farmer == null)
            {
                return NotFound();
            }

            var products = _context.Products.Where(p => p.UserId == id);

            _context.Products.RemoveRange(products);

            _context.Farmers.Remove(farmer);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ViewFarmers));
        }

        public async Task<IActionResult> ViewFarmers(string farmerId, string category, DateTime? productionDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            //get the user's role
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            var farmers = await _context.Farmers
                .Where(f => f.EmployeeId == user.Id)
                .ToListAsync();

            var viewModel = farmers.Select(f => new FarmerViewModel
            {
                Id = f.Id,
                FirstName = f.FirstName,
                LastName = f.LastName,
                Email = f.Email,
                Products = _context.Products.Where(p => p.UserId == f.Id).ToList()
            }).ToList();

            if (!string.IsNullOrEmpty(farmerId))
            {
                var selectedFarmer = viewModel.FirstOrDefault(f => f.Id == farmerId);
                if (selectedFarmer != null)
                {
                    var products = selectedFarmer.Products.AsQueryable();

                    if (!string.IsNullOrEmpty(category))
                    {
                        products = products.Where(p => p.Category == category);
                    }

                    if (productionDate.HasValue)
                    {
                        products = products.Where(p => p.ProductionDate.Date == productionDate.Value.Date);
                    }

                    selectedFarmer.Products = products.ToList();
                    selectedFarmer.SelectedCategory = category;
                    selectedFarmer.ProductionDate = productionDate;
                }
            }

            ViewData["UserId"] = user.Id;
            ViewData["UserRole"] = userRole; // Assuming you have a UserRole property

            return View(viewModel);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult FilterProducts(string farmerId, string category, DateTime? productionDate)
        {
            // Assuming you have a context or service to interact with the database
            var products = _context.Products.Where(p => p.UserId == farmerId).AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }

            if (productionDate.HasValue)
            {
                products = products.Where(p => p.ProductionDate.Date == productionDate.Value.Date);
            }

            var filteredProducts = products.ToList();

            return PartialView("_ProductsPartial", filteredProducts);
        }
    }

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

