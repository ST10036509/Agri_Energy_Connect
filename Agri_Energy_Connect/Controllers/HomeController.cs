using Agri_Energy_Connect.Areas.Identity.Data;
using Agri_Energy_Connect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Agri_Energy_Connect.Controllers
{
    //lock access to logged in users only
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userManager"></param>
        /// <param name="signInManager"></param>
        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            //setup logger and managers
            _logger = logger;
            this._userManager=userManager;
            this._signInManager=signInManager;
        }

        /// <summary>
        /// Index Event Handler
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
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
        /// Privacy Event Handler
        /// </summary>
        /// <returns></returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Error Event Handler
        /// </summary>
        /// <returns></returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
//---------------....oooOO0_END_OF_FILE_0OOooo....---------------\\