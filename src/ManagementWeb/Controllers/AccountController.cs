using IIS.FTP.ManagementWeb.Models;
using IIS.FTP.ManagementWeb.Services;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace IIS.FTP.ManagementWeb.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IApplicationServices _applicationServices;

        public AccountController(IApplicationServices applicationServices)
        {
            _applicationServices = applicationServices ?? throw new ArgumentNullException(nameof(applicationServices));
        }

        // GET: /Account/Login
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if user is an allowed admin
            var allowedAdmins = ConfigurationManager.AppSettings["AllowedAdmins"]?.Split(',') ?? new string[0];
            if (allowedAdmins.Length > 0 && !Array.Exists(allowedAdmins, admin => admin.Trim().Equals(model.Username, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("", "Access denied. User is not authorized to access this application.");
                return View(model);
            }

            // Validate credentials
            var isValid = await _applicationServices.ValidateUserAsync(model.Username, model.Password);

            if (isValid)
            {
                FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                
                if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        // GET: /Account/Logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }
    }
} 