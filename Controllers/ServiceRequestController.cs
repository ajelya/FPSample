using FPSample.Controllers.Data; 
using FPSample.Models.Entities;
using Microsoft.AspNetCore.Http; // Required for Session
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FPSample.Controllers
{
    public class ServiceRequestController : Controller
    {
        private readonly ApplicationDBContext _context;

        public ServiceRequestController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Create()
        {
            // 1. SECURITY LACK: Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.ServiceList = _context.Services.Where(s => s.IsEnabled).ToList();
            return View();
        }

        [HttpGet]
        public JsonResult GetPurposes(int serviceId)
        {
            var purposes = _context.ServicePurposes
                .Where(p => p.ServiceId == serviceId && p.IsEnabled)
                .Select(p => new {
                    // JavaScript is case-sensitive; usually uses lowercase 'p'
                    purposeId = p.PurposeId,
                    purposeName = p.PurposeName
                })
                .ToList();
            return Json(purposes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest request)
        {
            // 3. USER ID LACK: Get the logged-in ID from session
            int? loggedInUserId = HttpContext.Session.GetInt32("UserId");

            if (loggedInUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 4. VALIDATION LACK: Tell C# to ignore UserId during validation 
            // since we are assigning it manually below.
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                request.UserId = loggedInUserId.Value; // Assign the ID from session
                request.StatusId = 0;
                request.CreatedAt = DateTime.Now;

                _context.ServiceRequests.Add(request);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            // 5. DATA CONSISTENCY LACK: Filter enabled services only even on error
            ViewBag.ServiceList = _context.Services.Where(s => s.IsEnabled).ToList();
            return View(request);
        }
    }
}