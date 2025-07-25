using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.FTP.ManagementWeb.Models;
using IIS.FTP.ManagementWeb.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace IIS.FTP.ManagementWeb.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IApplicationServices _applicationServices;

        public UsersController(IApplicationServices applicationServices)
        {
            _applicationServices = applicationServices ?? throw new ArgumentNullException(nameof(applicationServices));
        }

        // GET: /Users
        public async Task<ActionResult> Index(string search, int page = 1)
        {
            var allUsers = await _applicationServices.GetAllUsersAsync();
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                allUsers = allUsers.Where(u => 
                    u.UserId.Contains(search) || 
                    u.DisplayName.Contains(search) ||
                    u.HomeDirectory.Contains(search)).ToList();
            }

            const int pageSize = 20;
            var totalCount = allUsers.Count();
            var users = allUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserListItemViewModel
                {
                    UserId = u.UserId,
                    DisplayName = u.DisplayName,
                    HomeDirectory = u.HomeDirectory,
                    PermissionCount = u.Permissions?.Count ?? 0
                })
                .ToList();

            var model = new UserListViewModel
            {
                Users = users,
                SearchTerm = search,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(model);
        }

        // GET: /Users/Create
        public ActionResult Create()
        {
            var model = new UserViewModel
            {
                Permissions = new List<PermissionViewModel>()
            };
            return View(model);
        }

        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User
            {
                UserId = model.UserId,
                DisplayName = model.DisplayName,
                HomeDirectory = model.HomeDirectory,
                Permissions = model.Permissions?.Select(p => new Permission
                {
                    Path = p.Path,
                    Read = p.Read,
                    Write = p.Write
                }).ToList() ?? new List<Permission>()
            };

            try
            {
                var success = await _applicationServices.CreateUserAsync(user, model.Password);
                if (success)
                {
                    TempData["SuccessMessage"] = $"User '{user.UserId}' created successfully.";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Failed to create user. Please check the logs for details.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating user: {ex.Message}");
            }

            return View(model);
        }

        // GET: /Users/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(400, "User ID is required");
            }

            var user = await _applicationServices.GetUserAsync(id);
            if (user == null)
            {
                return HttpNotFound($"User '{id}' not found");
            }

            var model = new UserViewModel
            {
                UserId = user.UserId,
                DisplayName = user.DisplayName,
                HomeDirectory = user.HomeDirectory,
                Permissions = user.Permissions?.Select(p => new PermissionViewModel
                {
                    Path = p.Path,
                    Read = p.Read,
                    Write = p.Write
                }).ToList() ?? new List<PermissionViewModel>()
            };

            return View(model);
        }

        // POST: /Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(UserViewModel model)
        {
            // Remove password validation for edit
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User
            {
                UserId = model.UserId,
                DisplayName = model.DisplayName,
                HomeDirectory = model.HomeDirectory,
                Permissions = model.Permissions?.Select(p => new Permission
                {
                    Path = p.Path,
                    Read = p.Read,
                    Write = p.Write
                }).ToList() ?? new List<Permission>()
            };

            try
            {
                var success = await _applicationServices.UpdateUserAsync(user);
                if (success)
                {
                    TempData["SuccessMessage"] = $"User '{user.UserId}' updated successfully.";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Failed to update user. Please check the logs for details.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating user: {ex.Message}");
            }

            return View(model);
        }

        // GET: /Users/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(400, "User ID is required");
            }

            var user = await _applicationServices.GetUserAsync(id);
            if (user == null)
            {
                return HttpNotFound($"User '{id}' not found");
            }

            return View(user);
        }

        // POST: /Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var success = await _applicationServices.DeleteUserAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = $"User '{id}' deleted successfully.";
                    return RedirectToAction("Index");
                }

                TempData["ErrorMessage"] = "Failed to delete user. Please check the logs for details.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // GET: /Users/ChangePassword/5
        public ActionResult ChangePassword(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(400, "User ID is required");
            }

            var model = new ChangePasswordViewModel
            {
                UserId = id
            };

            return View(model);
        }

        // POST: /Users/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _applicationServices.ChangePasswordAsync(
                    model.UserId, 
                    model.CurrentPassword, 
                    model.NewPassword);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Password changed successfully for user '{model.UserId}'.";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error changing password: {ex.Message}");
            }

            return View(model);
        }

        // AJAX endpoint for adding permission rows
        [HttpPost]
        public ActionResult AddPermissionRow(int index)
        {
            ViewData["Index"] = index;
            return PartialView("_PermissionRow", new PermissionViewModel());
        }
    }
} 