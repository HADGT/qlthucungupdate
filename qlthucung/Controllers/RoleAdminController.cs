using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qlthucung.Models;
using qlthucung.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qlthucung.Controllers
{
    public class RoleAdminController : Controller
    {
        private readonly UserManager<AppIdentityUser> _userManager;
        private readonly RoleManager<AppIdentityRole> _roleManager;
        public RoleAdminController(UserManager<AppIdentityUser> userManager, RoleManager<AppIdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(int? pageNumber, string searchTerm)
        {
            int pageSize = 5;
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<NguoiDungViewModel>();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Lọc người dùng theo FullName hoặc UserName chứa từ khóa (không phân biệt hoa thường)
                users = users
                    .Where(u => u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new NguoiDungViewModel
                {
                    FullName = user.FullName,
                    UserName = user.UserName,
                    BirthDate = user.BirthDate,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    LockoutEnabled = user.LockoutEnabled,
                    AccessFailedCount = user.AccessFailedCount,
                    Role = string.Join(", ", roles)
                });
            }
            // Truyền lại searchTerm để giữ giá trị tìm kiếm khi phân trang
            ViewBag.SearchTerm = searchTerm;
            var sortedList = userList.OrderBy(u => u.UserName).ToList(); // Không dùng AsQueryable
            var paginatedUsers = PaginatedList<NguoiDungViewModel>.Create(sortedList, pageNumber ?? 1, pageSize);
            return View(paginatedUsers);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRole(string userName, string newRole)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, newRole);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> LockUser(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UnlockUser(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userName = HttpContext.Session.GetString("usernameToChangePwd");
            if (string.IsNullOrEmpty(userName))
            {
                TempData["Error"] = "Không tìm thấy người dùng cần đổi mật khẩu.";
                return RedirectToAction("SignIn", "Security");
            }

            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                TempData["Error"] = "Tài khoản không tồn tại.";
                return RedirectToAction("SignIn", "Security");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                user.ForceChangePassword = false;
                await _userManager.UpdateAsync(user);
                HttpContext.Session.Remove("usernameToChangePwd");

                TempData["Success"] = "Đổi mật khẩu thành công. Hãy đăng nhập lại.";
                return RedirectToAction("SignIn", "Security");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        public IActionResult ForgotPasswordSuccess()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == model.UserName);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                return View(model);
            }

            // Kiểm tra nếu người dùng là admin
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("Admin"))
            {
                ModelState.AddModelError("", "Không thể reset mật khẩu cho tài khoản Admin.");
                return View(model);
            }

            // Tạo mật khẩu giả
            var tempPassword = GenerateRandomPassword();

            // Reset mật khẩu
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Không thể tạo mật khẩu mới.");
                return View(model);
            }

            // Bắt buộc người dùng đổi mật khẩu sau khi đăng nhập
            user.ForceChangePassword = true;
            await _userManager.UpdateAsync(user);

            // Lưu mật khẩu tạm thời để hiển thị
            ViewBag.TempPassword = tempPassword;

            // Option: lưu tạm tên người dùng để xử lý ở ChangePassword nếu cần
            HttpContext.Session.SetString("usernameToChangePwd", user.UserName);

            return View("ForgotPasswordSuccess");
        }


        private string GenerateRandomPassword(int length = 15)
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "1234567890";
            const string special = "!@#$%&*";
            const string valid = lowercase + uppercase + digits + special;

            var res = new StringBuilder();
            var rnd = new Random();

            // Đảm bảo ít nhất 1 ký tự mỗi loại
            res.Append(lowercase[rnd.Next(lowercase.Length)]);
            res.Append(uppercase[rnd.Next(uppercase.Length)]);
            res.Append(digits[rnd.Next(digits.Length)]);
            res.Append(special[rnd.Next(special.Length)]);

            // Thêm các ký tự còn lại từ valid
            for (int i = 4; i < length; i++)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }

            // Trộn ngẫu nhiên các ký tự
            var passwordArray = res.ToString().ToCharArray();
            var shuffledPassword = new string(passwordArray.OrderBy(x => rnd.Next()).ToArray());

            return shuffledPassword;
        }
    }
}


