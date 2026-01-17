using FPSample.Controllers.Data;
using FPSample.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

 
            var myRequests = await _context.ServiceRequests
                .Include(r => r.Service)
                .Include(r => r.Histories)
                    .ThenInclude(h => h.Admin)
                .Where(r => r.UserId == userId)
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

            ViewBag.ServiceList = _context.Services.Where(s => s.IsEnabled).ToList();
            return View();
        }

        // GET: ServiceRequest/GetPurposes?serviceId=X
        [HttpGet]
        public async Task<JsonResult> GetPurposes(int serviceId)
        {
            var purposes = await _context.ServicePurposes
                .Where(p => p.ServiceId == serviceId)
                .Select(p => new
                {
                    purposeId = p.PurposeId,
                    purposeName = p.PurposeName
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

            
            ModelState.Remove("UserId");
            ModelState.Remove("StatusId");
            ModelState.Remove("UploadPath");
            ModelState.Remove("Service");     
            ModelState.Remove("User");        
            ModelState.Remove("Histories");   

            if (ModelState.IsValid)
            {
                // Handle File Upload
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
                request.StatusId = 0;
                request.CreatedAt = DateTime.Now;

                _context.ServiceRequests.Add(request);
                await _context.SaveChangesAsync();

                return RedirectToAction("MyRequests");
            }


            ViewBag.ServiceList = _context.Services.Where(s => s.IsEnabled).ToList();
            return View(request);
        }
    }
}