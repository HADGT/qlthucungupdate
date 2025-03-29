using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qlthucung.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qlthucung.Controllers
{
    public class DichVuController : Controller
    {
        private readonly AppDbContext _context;

        public DichVuController(AppDbContext context)
        {
            _context = context;
        }
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

        public IActionResult Datlich()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DatLich([Bind("Hoten,Email,Sdt,Diachi,Trangthai,Tendichvu,Ngaydat,Makh")] DichVu model)
        {
            string khachHangName = HttpContext.Session.GetString("username");

            if (!string.IsNullOrEmpty(khachHangName))
            {
                var khachHang = _context.KhachHangs.FirstOrDefault(k => k.Tendangnhap == khachHangName);
                if (khachHang != null)
                {
                    model.Makh = khachHang.Makh; // Gán Makh vào model
                    model.Trangthai = "Đang chờ xử lý";
                }
            }

            if (ModelState.IsValid)
            {
                _context.DichVus.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}
