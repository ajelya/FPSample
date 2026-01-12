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

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch statuses for the dropdown
            ViewBag.StatusList = await _context.Statuses.ToListAsync();

            // Fetch requests and include History/Admin data so the Admin can see who last processed it
            var requests = await _context.ServiceRequests
                .Include(r => r.Histories)
                    .ThenInclude(h => h.Admin)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int requestId, int newStatusId)
        {
            var request = await _context.ServiceRequests.FindAsync(requestId);

            // IMPORTANT: Ensure your Login code sets "AdminId" in the session
            int? adminId = HttpContext.Session.GetInt32("AdminId");

            if (request != null && adminId != null)
            {
                // 1. Update the main request status
                request.StatusId = newStatusId;

                // 2. Create the History log entry
                var history = new History
                {
                    RequestId = requestId,
                    AdminId = adminId.Value,
                    StatusId = newStatusId,
                    UpdatedAt = DateTime.Now
                };

                _context.Histories.Add(history);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}