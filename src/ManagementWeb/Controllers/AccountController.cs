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

        // Parameterless constructor for Unity IoC container fallback
        public AccountController()
        {
            // This constructor is used when Unity IoC container fails to resolve dependencies
            // It will be called by MVC's DefaultControllerActivator as a fallback
            // Note: _applicationServices will be null in this case, so methods should handle it gracefully
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
            // Debug logging to file
            try
            {
                System.IO.Directory.CreateDirectory(@"C:\temp");
                System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                    $"{DateTime.Now}: Login attempt - Username: {model?.Username}, Services: {_applicationServices != null}\n");
            }
            catch { }

            if (!ModelState.IsValid)
            {
                try
                {
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: ModelState is invalid\n");
                }
                catch { }
                return View(model);
            }

            // Check if Unity IoC container failed to inject dependencies
            if (_applicationServices == null)
            {
                try
                {
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: ApplicationServices is NULL - Unity IoC injection failed\n");
                }
                catch { }
                ModelState.AddModelError("", "System configuration error. Please contact administrator.");
                return View(model);
            }

            // Check if user is an allowed admin
            var allowedAdmins = ConfigurationManager.AppSettings["AllowedAdmins"]?.Split(',') ?? new string[0];
            try
            {
                System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                    $"{DateTime.Now}: AllowedAdmins config: [{string.Join(",", allowedAdmins)}], Username: {model.Username}\n");
            }
            catch { }
            
            if (allowedAdmins.Length > 0 && !Array.Exists(allowedAdmins, admin => admin.Trim().Equals(model.Username, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: Access denied - user not in AllowedAdmins list\n");
                }
                catch { }
                ModelState.AddModelError("", "Access denied. User is not authorized to access this application.");
                return View(model);
            }

            // Validate credentials
            try
            {
                System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                    $"{DateTime.Now}: Calling ValidateUserAsync for username: {model.Username}\n");
            }
            catch { }
            
            var isValid = await _applicationServices.ValidateUserAsync(model.Username, model.Password);

            try
            {
                System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                    $"{DateTime.Now}: ValidateUserAsync result: {isValid}\n");
            }
            catch { }

            if (isValid)
            {
                try
                {
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: Login successful - setting auth cookie\n");
                }
                catch { }
                
                FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                
                if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                    $"{DateTime.Now}: Login failed - invalid username or password\n");
            }
            catch { }
            
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