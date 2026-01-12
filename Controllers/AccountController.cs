using FPSample.Controllers.Data;
using FPSample.Models;
using FPSample.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // --- LOGIN FEATURES ---

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Check Admin table first
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Username == model.Username && a.AdminPassword == model.Password);

                if (admin != null)
                {
                    HttpContext.Session.Clear();
                    // We store AdminId and set Role to "Admin"
                    HttpContext.Session.SetInt32("AdminId", admin.AdminId);
                    HttpContext.Session.SetString("UserName", admin.Username);
                    HttpContext.Session.SetString("UserRole", "Admin");

                    return RedirectToAction("Index", "Admin");
                }

                // 2. Check User table if no admin was found
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == model.Password);

                if (user != null)
                {
                    HttpContext.Session.Clear();
                    // We store UserId and set Role to "User"
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    HttpContext.Session.SetString("UserName", user.FirstName);
                    HttpContext.Session.SetString("UserRole", "User");

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid username or password.");
            }
            return View(model);
        }

        // --- PROFILE EDIT FEATURES ---

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // If an admin tries to access 'Profile', send them to login or their own dashboard
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User updatedUser)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            // Security check to ensure user is editing their own data
            if (updatedUser.UserId != userId) return Forbid();

            // Allow profile update without changing password
            if (string.IsNullOrEmpty(updatedUser.Password))
            {
                ModelState.Remove("Password");
            }

            if (ModelState.IsValid)
            {
                var userInDb = await _context.Users.FindAsync(userId);
                if (userInDb != null)
                {
                    // Update Name Information
                    userInDb.FirstName = updatedUser.FirstName;
                    userInDb.MiddleName = updatedUser.MiddleName;
                    userInDb.LastName = updatedUser.LastName;
                    userInDb.Suffix = updatedUser.Suffix;

                    // Update Personal Details
                    userInDb.DateOfBirth = updatedUser.DateOfBirth;
                    userInDb.Sex = updatedUser.Sex;
                    userInDb.CivilStatus = updatedUser.CivilStatus;
                    userInDb.Religion = updatedUser.Religion;

                    // Update Address & Residency
                    userInDb.HouseNoStreet = updatedUser.HouseNoStreet;
                    userInDb.Barangay = updatedUser.Barangay;
                    userInDb.City = updatedUser.City;
                    userInDb.Province = updatedUser.Province;
                    userInDb.StayYears = updatedUser.StayYears;
                    userInDb.StayMonths = updatedUser.StayMonths;

                    // Update Contact & Voter Info
                    userInDb.ContactNo = updatedUser.ContactNo;
                    userInDb.Email = updatedUser.Email;
                    userInDb.IsVoter = updatedUser.IsVoter;

                    // Update Account Credentials
                    userInDb.Username = updatedUser.Username;

                    if (!string.IsNullOrEmpty(updatedUser.Password))
                    {
                        userInDb.Password = updatedUser.Password;
                    }

                    _context.Update(userInDb);
                    await _context.SaveChangesAsync();

                    // Refresh Session name in case FirstName changed
                    HttpContext.Session.SetString("UserName", userInDb.FirstName);

                    TempData["Success"] = "Your profile has been updated successfully!";
                    return RedirectToAction("Profile");
                }
            }
            return View(updatedUser);
        }

        // --- LOGOUT ---

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}