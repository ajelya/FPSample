using FPSample.Controllers.Data;
using FPSample.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace FPSample.Controllers
{
    public class ServiceRequestController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ServiceRequestController(ApplicationDBContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: ServiceRequest/MyRequests
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch data: Include Histories and Admin for the status timeline
            var myRequests = await _context.ServiceRequests
                .Where(r => r.UserId == userId)
                .Include(r => r.Histories)
                    .ThenInclude(h => h.Admin)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(myRequests);
        }

        // GET: ServiceRequest/Create
        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Populate the dropdown for Services
            ViewBag.ServiceList = _context.Services.Where(s => s.IsEnabled).ToList();
            return View();
        }

        // GET: ServiceRequest/GetPurposes?serviceId=X
        // FIXED: Renamed anonymous object properties to match your JavaScript
        [HttpGet]
        public async Task<JsonResult> GetPurposes(int serviceId)
        {
            var purposes = await _context.ServicePurposes
                .Where(p => p.ServiceId == serviceId)
                .Select(p => new
                {
                    purposeId = p.PurposeId,   // JavaScript expects lowercase 'p'
                    purposeName = p.PurposeName // JavaScript expects lowercase 'p'
                })
                .ToListAsync();

            return Json(purposes);
        }

        // POST: ServiceRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest request)
        {
            int? loggedInUserId = HttpContext.Session.GetInt32("UserId");

            if (loggedInUserId == null) return RedirectToAction("Login", "Account");

            // Remove non-user-input fields from validation
            ModelState.Remove("UserId");
            ModelState.Remove("StatusId");
            ModelState.Remove("UploadPath");

            if (ModelState.IsValid)
            {
                // Handle File Upload for Barangay ID
                if (request.ProfilePicture != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/ids");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + request.ProfilePicture.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.ProfilePicture.CopyToAsync(fileStream);
                    }
                    request.UploadPath = "/uploads/ids/" + uniqueFileName;
                }

                request.UserId = loggedInUserId.Value;
                request.StatusId = 0; // 0 = Pending
                request.CreatedAt = DateTime.Now;

                _context.ServiceRequests.Add(request);
                await _context.SaveChangesAsync();

                return RedirectToAction("MyRequests");
            }

            // If we got here, something failed; reload the services for the view
            ViewBag.ServiceList = _context.Services.Where(s => s.IsEnabled).ToList();
            return View(request);
        }
    }
}