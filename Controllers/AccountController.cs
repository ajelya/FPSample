using FPSample.Controllers.Data;
using FPSample.Models;
using FPSample.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FPSample.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDBContext _context;

        public AccountController(ApplicationDBContext context)
        {
            _context = context;
        }

        // ================= LOGIN =================
        [HttpGet]
        public IActionResult Login()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Admin") return RedirectToAction("Index", "Admin");
            if (role == "User") return RedirectToAction("Home", "Account");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check Admin Table
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Username == model.Username && a.AdminPassword == model.Password);

                if (admin != null)
                {
                    HttpContext.Session.Clear();
                    HttpContext.Session.SetInt32("AdminId", admin.AdminId);
                    // Use Username or a specific Display Name for the sidebar badge
                    HttpContext.Session.SetString("UserName", admin.Username);
                    HttpContext.Session.SetString("UserRole", "Admin");

                    return RedirectToAction("Index", "Admin");
                }

                // Check User Table
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == model.Password);

                if (user != null)
                {
                    // Check if account is disabled by admin
                    if (!user.IsActive)
                    {
                        ModelState.AddModelError("", "Your account has been disabled. Please contact the administrator.");
                        return View(model);
                    }

                    HttpContext.Session.Clear();
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    HttpContext.Session.SetString("UserName", user.FirstName);
                    HttpContext.Session.SetString("UserRole", "User");

                    return RedirectToAction("Home", "Account");
                }

                ModelState.AddModelError("", "Invalid username or password.");
            }
            return View(model);
        }

        // ================= SIGNUP =================
        [HttpGet]
        public IActionResult Signup() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(User model, string confirmPassword)
        {
            if (model.Password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
            }

            // Remove non-user-input fields from validation
            ModelState.Remove("Role");
            ModelState.Remove("IsActive");
            ModelState.Remove("ServiceRequests");

            if (ModelState.IsValid)
            {
                try
                {
                    bool userExists = await _context.Users.AnyAsync(u => u.Username == model.Username);
                    if (userExists)
                    {
                        ModelState.AddModelError("Username", "Username is already taken.");
                        return View(model);
                    }

                    model.IsActive = true; // Default to active
                    _context.Users.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Account created successfully!";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Database Error: " + (ex.InnerException?.Message ?? ex.Message));
                }
            }
            return View(model);
        }

        // ================= USER PAGES =================

        public IActionResult Home()
        {
            // Security: Ensure only Residents can access User Home
            if (HttpContext.Session.GetString("UserRole") != "User") return RedirectToAction("Login");
            return View();
        }

        public async Task<IActionResult> MyRequests()
        {
            if (HttpContext.Session.GetString("UserRole") != "User") return RedirectToAction("Login");

            int? userId = HttpContext.Session.GetInt32("UserId");

            var requests = await _context.ServiceRequests
                                 .Include(r => r.Service)
                                 .Include(r => r.Histories)
                                    .ThenInclude(h => h.Admin)
                                 .Where(r => r.UserId == userId)
                                 .OrderByDescending(r => r.CreatedAt)
                                 .AsNoTracking()
                                 .ToListAsync();

            return View("~/Views/ServiceRequest/MyRequests.cshtml", requests);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || HttpContext.Session.GetString("UserRole") != "User")
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User updatedUser)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            if (string.IsNullOrEmpty(updatedUser.Password)) ModelState.Remove("Password");

            if (ModelState.IsValid)
            {
                var userInDb = await _context.Users.FindAsync(userId);
                if (userInDb != null)
                {
                    // Map updated fields
                    userInDb.FirstName = updatedUser.FirstName;
                    userInDb.MiddleName = updatedUser.MiddleName;
                    userInDb.LastName = updatedUser.LastName;
                    userInDb.Suffix = updatedUser.Suffix;
                    userInDb.DateOfBirth = updatedUser.DateOfBirth;
                    userInDb.Sex = updatedUser.Sex;
                    userInDb.CivilStatus = updatedUser.CivilStatus;
                    userInDb.Religion = updatedUser.Religion;
                    userInDb.HouseNoStreet = updatedUser.HouseNoStreet;
                    userInDb.Barangay = updatedUser.Barangay;
                    userInDb.City = updatedUser.City;
                    userInDb.Province = updatedUser.Province;
                    userInDb.StayYears = updatedUser.StayYears;
                    userInDb.StayMonths = updatedUser.StayMonths;
                    userInDb.IsVoter = updatedUser.IsVoter;
                    userInDb.ContactNo = updatedUser.ContactNo;
                    userInDb.Email = updatedUser.Email;
                    userInDb.Username = updatedUser.Username;

                    if (!string.IsNullOrEmpty(updatedUser.Password)) userInDb.Password = updatedUser.Password;

                    _context.Update(userInDb);
                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetString("UserName", userInDb.FirstName);
                    TempData["Success"] = "Profile Updated Successfully!";
                    return RedirectToAction("Profile");
                }
            }
            return View(updatedUser);
        }

        // ================= LOGOUT =================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}