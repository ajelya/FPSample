using FPSample.Controllers.Data;
using FPSample.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FPSample.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDBContext _context;

        public AdminController(ApplicationDBContext context)
        {
            _context = context;
        }

        // --- HELPER: CHECK ADMIN ACCESS ---
        private bool IsNotAdmin() => HttpContext.Session.GetString("UserRole") != "Admin";

        // --- DASHBOARD (Summaries Only) ---
        public async Task<IActionResult> Index()
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");

            var viewModel = new AdminDashboardVM
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalRequests = await _context.ServiceRequests.CountAsync(),
                ApprovedRequests = await _context.ServiceRequests.CountAsync(r => r.StatusId == 2),
                ReadyToClaim = await _context.ServiceRequests.CountAsync(r => r.StatusId == 3),
                RejectedRequests = await _context.ServiceRequests.CountAsync(r => r.StatusId == 4),
            
                ActiveRequests = new List<dynamic>()
            };

            return View(viewModel);
        }

        // --- NEW: DEDICATED SERVICE REQUESTS PAGE ---
        public async Task<IActionResult> ActiveRequests()
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");

            ViewBag.StatusList = await _context.Statuses.ToListAsync();
            var allServices = await _context.Services.ToListAsync();

            var rawRequests = await (from req in _context.ServiceRequests
                                     join u in _context.Users on req.UserId equals u.UserId into userGroup
                                     from user in userGroup.DefaultIfEmpty()
                                     join s in _context.Statuses on req.StatusId equals s.StatusId into statusGroup
                                     from status in statusGroup.DefaultIfEmpty()
                                     select new
                                     {
                                         Request = req,
                                         User = user,
                                         ResidentName = user != null ? user.FirstName + " " + user.LastName : "Unknown",
                                         CurrentStatusName = status != null ? status.StatusName : "Pending"
                                     }).OrderByDescending(r => r.Request.RequestId).ToListAsync();

            var requestsData = rawRequests.Select(item => (dynamic)new
            {
                item.Request,
                item.User,
                item.ResidentName,
                item.CurrentStatusName,
                RequestedDocument = allServices.FirstOrDefault(s => s.ServiceId == item.Request.ServiceId)?.ServiceName ?? "General Request"
            }).ToList();

            return View(requestsData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int requestId, int newStatusId, DateTime? claimDate, TimeSpan? claimTime)
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");

            var request = await _context.ServiceRequests.FindAsync(requestId);
            int? adminId = HttpContext.Session.GetInt32("AdminId");

            if (request != null)
            {
                request.StatusId = newStatusId;
                if (claimDate.HasValue) request.DateToClaim = claimDate.Value;
                if (claimTime.HasValue) request.TimeToClaim = claimTime.Value;

                if (adminId != null)
                {
                    _context.Histories.Add(new History
                    {
                        RequestId = requestId,
                        AdminId = adminId.Value,
                        StatusId = newStatusId,
                        UpdatedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Request #{requestId} updated successfully.";
            }

            // Redirect back to ActiveRequests instead of Index
            return RedirectToAction(nameof(ActiveRequests));
        }

        // --- RESIDENT MANAGEMENT ---
        public async Task<IActionResult> Users(string searchTerm)
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");

            var usersQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowerSearch = searchTerm.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.ToLower().Contains(lowerSearch) ||
                    u.LastName.ToLower().Contains(lowerSearch) ||
                    u.Email.ToLower().Contains(lowerSearch) ||
                    u.ContactNo.Contains(lowerSearch));
            }

            ViewData["CurrentFilter"] = searchTerm;
            var result = await usersQuery.OrderByDescending(u => u.UserId).ToListAsync();
            return View(result);
        }

        public async Task<IActionResult> ViewProfile(int id)
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View("ViewProfile", user);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(User updatedUser)
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.Update(updatedUser);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Resident information updated.";
                return RedirectToAction(nameof(Users));
            }
            return View(updatedUser);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"User {(user.IsActive ? "Activated" : "Deactivated")} successfully.";
            }
            return RedirectToAction(nameof(Users));
        }

        // --- SERVICE MANAGEMENT ---
        public async Task<IActionResult> Services()
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");
            var services = await _context.Services.Include(s => s.ServicePurposes).ToListAsync();
            return View(services);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertService(int? ServiceId, string ServiceName, string Description, List<string> Purposes)
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(ServiceName)) return BadRequest("Service Name is required.");

            Service service;
            if (ServiceId == null || ServiceId == 0)
            {
                service = new Service { ServiceName = ServiceName, Description = Description, IsEnabled = true };
                _context.Services.Add(service);
            }
            else
            {
                service = await _context.Services.Include(s => s.ServicePurposes).FirstOrDefaultAsync(s => s.ServiceId == ServiceId);
                if (service == null) return NotFound();

                service.ServiceName = ServiceName;
                service.Description = Description;

                if (service.ServicePurposes != null)
                    _context.ServicePurposes.RemoveRange(service.ServicePurposes);
            }

            await _context.SaveChangesAsync();

            if (Purposes != null && Purposes.Any())
            {
                foreach (var p in Purposes.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    _context.ServicePurposes.Add(new ServicePurpose
                    {
                        ServiceId = service.ServiceId,
                        PurposeName = p
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Service details saved.";
            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleServiceStatus(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                service.IsEnabled = !service.IsEnabled;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.Include(s => s.ServicePurposes).FirstOrDefaultAsync(s => s.ServiceId == id);
            if (service != null)
            {
                if (service.ServicePurposes != null) _context.ServicePurposes.RemoveRange(service.ServicePurposes);
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Services));
        }

        // --- ADMIN OWN PROFILE SETTINGS ---
        [HttpGet]
        public async Task<IActionResult> AdminProfile()
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");

            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return RedirectToAction("Login", "Account");

            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null) return RedirectToAction("Logout", "Account");

            return View("AdminProfile", admin);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (IsNotAdmin()) return RedirectToAction("Login", "Account");

            var adminId = HttpContext.Session.GetInt32("AdminId");
            var admin = await _context.Admins.FindAsync(adminId);

            if (admin == null) return NotFound();

            if (admin.AdminPassword != currentPassword)
            {
                TempData["Error"] = "Incorrect current password.";
                return RedirectToAction(nameof(AdminProfile));
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                return RedirectToAction(nameof(AdminProfile));
            }

            admin.AdminPassword = newPassword;
            _context.Update(admin);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction(nameof(AdminProfile));
        }
    }
}