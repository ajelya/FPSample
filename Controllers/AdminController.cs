using FPSample.Controllers.Data;
using FPSample.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FPSample.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDBContext _context;

        public AdminController(ApplicationDBContext context)
        {
            _context = context;
        }

        // --- DASHBOARD ---
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalRequests = await _context.ServiceRequests.CountAsync();
            ViewBag.ApprovedRequests = await _context.ServiceRequests.CountAsync(r => r.StatusId == 2);
            ViewBag.RejectedRequests = await _context.ServiceRequests.CountAsync(r => r.StatusId == 4);
            ViewBag.PickedUpRequests = await _context.ServiceRequests.CountAsync(r => r.StatusId == 3);
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
                                     }).ToListAsync();

            var requestsData = rawRequests.Select(item => new {
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
        public async Task<IActionResult> UpdateStatus(int requestId, int newStatusId, DateTime? pickupSchedule)
        {
            var request = await _context.ServiceRequests.FindAsync(requestId);
            int? adminId = HttpContext.Session.GetInt32("AdminId");

            if (request != null && adminId != null)
            {
                request.StatusId = newStatusId;
                if (pickupSchedule.HasValue)
                {
                    request.DateToClaim = pickupSchedule.Value.Date;
                    request.TimeToClaim = pickupSchedule.Value.TimeOfDay;
                }
                _context.Histories.Add(new History
                {
                    RequestId = requestId,
                    AdminId = adminId.Value,
                    StatusId = newStatusId,
                    UpdatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Status updated.";
            }
            return RedirectToAction(nameof(Index));
        }

        // --- USER MANAGEMENT ---
        public async Task<IActionResult> Users(string searchString)
        {
            var usersQuery = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
                usersQuery = usersQuery.Where(u => u.FirstName.Contains(searchString) || u.LastName.Contains(searchString));

            return View(await usersQuery.OrderByDescending(u => u.UserId).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null) { user.IsActive = !user.IsActive; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Users));
        }

        // --- SERVICE & PURPOSE MANAGEMENT ---
        public async Task<IActionResult> Services()
        {
            // The .Include fixes the error by loading the child purposes
            var services = await _context.Services.Include(s => s.ServicePurposes).ToListAsync();
            return View(services);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertService(int? ServiceId, string ServiceName, string Description, List<string> Purposes)
        {
            if (string.IsNullOrEmpty(ServiceName)) return BadRequest("Name required.");

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

                // Remove old purposes to refresh them
                if (service.ServicePurposes != null)
                    _context.ServicePurposes.RemoveRange(service.ServicePurposes);
            }

            await _context.SaveChangesAsync(); // Saves the service and clears old purposes

            // Add new purposes
            if (Purposes != null)
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

            TempData["Success"] = "Service and Purposes saved.";
            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleServiceStatus(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null) { service.IsEnabled = !service.IsEnabled; await _context.SaveChangesAsync(); }
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
                TempData["Success"] = "Permanently deleted.";
            }
            return RedirectToAction(nameof(Services));
        }
        // --- ADMIN PROFILE / PASSWORD MANAGEMENT ---

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return RedirectToAction("Login", "Account");

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New password and confirmation do not match.");
                return View();
            }

            // Fetch the admin record (Assuming your Admin entity is in _context.Admins)
            var admin = await _context.Admins.FindAsync(adminId);

            if (admin == null || admin.AdminPassword != currentPassword) // In production, use password hashing!
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View();
            }

            admin.AdminPassword = newPassword;
            _context.Update(admin);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}