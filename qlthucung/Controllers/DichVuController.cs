using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using qlthucung.Models;
using qlthucung.Models.mail;
using qlthucung.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qlthucung.Controllers
{
    public class DichVuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<AppIdentityUser> _userManager;

        public DichVuController(AppDbContext context, IEmailSender emailSender, UserManager<AppIdentityUser> userManager)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        [Authorize(Roles = "User")]
        public IActionResult Index()
        {
            string khachHangName = HttpContext.Session.GetString("username");

            if (string.IsNullOrEmpty(khachHangName))
            {
                return RedirectToAction("SignIn", "Security");
            }

            ViewBag.KhachHangName = khachHangName;
            return View();
        }

        [Authorize(Roles = "User")]
        public IActionResult Datlich()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DatLich([Bind("Hoten,Email,Sdt,Diachi,Trangthai,Tendichvu,Ngaydat,Makh")] DichVu model)
        {
            string khachHangName = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(khachHangName))
            {
                return RedirectToAction("SignIn", "Security");
            }
            else
            {
                var khachHang = _context.KhachHangs.FirstOrDefault(k => k.Tendangnhap == khachHangName);
                if (khachHang != null)
                {
                    model.Makh = khachHang.Makh; // Gán Makh vào model
                    model.Trangthai = "Đang chờ xử lý";
                }
            }

            // Kiểm tra ngày đặt không được là ngày trong quá khứ
            if (model.Ngaydat.HasValue && model.Ngaydat.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError("Ngaydat", "Ngày đặt không được nhỏ hơn ngày hiện tại.");
                return View(model);  // Trả về view với lỗi để người dùng sửa
            }

            if (ModelState.IsValid)
            {
                _context.DichVus.Add(model);
                _context.SaveChanges();
                // Gửi email thông báo
                var user = _context.AspNetUsers.FirstOrDefault(p => p.UserName == khachHangName);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    string subject = "Thông báo đặt lịch dịch vụ thành công";
                    string body = $@"
                        <p>Xin chào {user.UserName},</p>
                        <p>Bạn đã đặt lịch dịch vụ <strong>{model.Tendichvu}</strong> thành công.</p>
                        <p>Ngày đặt: {model.Ngaydat?.ToString("dd/MM/yyyy")}</p>
                        <p>Trạng thái: {model.Trangthai}</p>
                        <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                    ";

                    await _emailSender.SendEmailAsync(user.Email, subject, body);
                }
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendUserInfoEmail()
        {
            string username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("SignIn", "Security");

            var user = _context.AspNetUsers.FirstOrDefault(p => p.UserName == username);
            if (user == null || string.IsNullOrEmpty(user.Email))
                return BadRequest("Không tìm thấy người dùng hoặc email.");

            // Tạo token xác nhận email
            var identityUser = await _userManager.FindByNameAsync(username);
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);

            // Tạo link xác nhận
            var confirmationLink = Url.Action("ConfirmEmail", "DichVu", new
            {
                userId = identityUser.Id,
                token = token
            }, protocol: HttpContext.Request.Scheme);

            // Gửi email
            var email = new MailMess
            {
                To = user.Email,
                Subject = "Xác nhận email",
                Body = $"Xin chào {user.UserName},<br/>Vui lòng xác nhận email bằng cách <a href='{confirmationLink}'>bấm vào đây</a>."
            };

            await _emailSender.SendEmailAsync(email.To, email.Subject, email.Body);
            return Ok("Email xác nhận đã được gửi.");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return BadRequest("Yêu cầu không hợp lệ.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("Không tìm thấy người dùng.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return View("ConfirmEmailSuccess"); // Tạo view này
            }

            return View("Error"); // Tạo view lỗi nếu xác thực thất bại
        }


        [HttpGet]
        public IActionResult UserInfo()
        {
            string username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("SignIn", "Security");
            }
            var user = _context.AspNetUsers
                        .Where(u => u.UserName == username)
                        .Select(u => new UserInfoViewModel
                        {
                            FullName = u.FullName,
                            UserName = u.UserName,
                            BirthDate = u.BirthDate,
                            Email = u.Email,
                            EmailConfirmed = u.EmailConfirmed,
                            PhoneNumber = u.PhoneNumber,
                            PhoneNumberConfirmed = u.PhoneNumberConfirmed
                        })
                        .FirstOrDefault();

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
    }
}
