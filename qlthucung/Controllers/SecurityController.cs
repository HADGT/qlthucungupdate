using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using qlthucung.Models;
using qlthucung.Security;
using Microsoft.AspNetCore.Http;

namespace qlthucung.Controllers
{
    public class SecurityController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppIdentityUser> userManager;
        private readonly RoleManager<AppIdentityRole> roleManager;
        private readonly SignInManager<AppIdentityUser> signInManager;

        public SecurityController(UserManager<AppIdentityUser> userManager,
            RoleManager<AppIdentityRole> roleManager,
            SignInManager<AppIdentityUser> signInManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Register register)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem username đã tồn tại chưa
                var existingUser = userManager.FindByNameAsync(register.UserName).Result;
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
                    return View(register); // Trả về ngay nếu username đã tồn tại
                }

                // Kiểm tra role "Admin" đã tồn tại chưa
                if (!roleManager.RoleExistsAsync("Admin").Result)
                {
                    var role = new AppIdentityRole
                    {
                        Name = "Admin",
                        Description = "Admin can Perform CRUD Employee"
                    };
                    var roleResult = roleManager.CreateAsync(role).Result;
                }

                // Tạo user mới
                var user = new AppIdentityUser
                {
                    UserName = register.UserName,
                    Email = register.Email,
                    FullName = register.FullName,
                    BirthDate = register.BirthDate
                };

                var result = userManager.CreateAsync(user, register.Password).Result;
                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user, "Admin").Wait();
                    return RedirectToAction("SignIn", "Security");
                }
                else
                {
                    // Thêm từng lỗi vào ModelState để hiển thị
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return View(register);
        }


        [HttpGet]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignIn(SignIn signIn)
        {
            if (ModelState.IsValid)
            {
                var result = signInManager.PasswordSignInAsync(signIn.UserName, signIn.Password, signIn.RememberMe, false).Result;

                if (result.Succeeded)
                {
                    HttpContext.Session.SetString("username", signIn.UserName);
                    return RedirectToAction("Index", "Home");
                }

                else
                    TempData["LoginErr"] = "Tên tài khoản hoặc mật khẩu không chính xác!";
            }
            return View(signIn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult SignOut()
        {
            signInManager.SignOutAsync().Wait();
            HttpContext.Session.Remove("username");
            return RedirectToAction("SignIn", "Security");

        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
