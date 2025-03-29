using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using qlthucung.Models;
using RestSharp;

namespace qlthucung.Controllers
{
    [Authorize(Roles = "Manager,Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public AdminController(AppDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        //Show drop list danh muc
        private void showDropList()
        {
            List<SelectListItem> list = (from c in _context.DanhMucs
                                         select new SelectListItem()
                                         {
                                             Text = c.Tendanhmuc,
                                             Value = c.IdDanhmuc.ToString()
                                         }).Distinct().ToList();
            ViewBag.ShowDropList = list;
        }

        // GET: Admin

        public async Task<IActionResult> Index()
        {

            //Dashboard
            ViewBag.ThongKeSL = ThongKeSL();
            ViewBag.ThongKeDonHang = ThongKeDonHang();
            ViewBag.ThongKeKH = ThongKeKhachHang();
            ViewBag.TongDoanhThu = _context.ChiTietDonHangs
                .Where(m => m.Status == 1)
                .Sum(
                n => n.Soluong * n.Gia
            ).Value;
            return View();
        }
        public async Task<IActionResult> ListDanhMuc(int? pageNumber, string searchTerm)
        {
            const int pageSize = 5;
            var appDbContext = _context.SanPhams.Include(s => s.IdDanhmucNavigation).Include(s => s.IdthuvienNavigation);
            if (searchTerm != null)
            {
                var lstSP = _context.SanPhams.Where(n => n.Tensp.Contains(searchTerm));
                ViewBag.searchTerm = searchTerm;
                var paginatedProducts = await PaginatedList<SanPham>.CreateAsync(lstSP, pageNumber ?? 1, pageSize);
                return View(paginatedProducts);
            }
            else
            {
                var lstSP = (from s in _context.SanPhams select s).OrderBy(m => m.Masp);
                var paginatedProducts = await PaginatedList<SanPham>.CreateAsync(lstSP, pageNumber ?? 1, pageSize);
                return View(paginatedProducts);
            }
        }
        public ActionResult ListDanhMucSP(string selectedParentName, string searchTerm, int page = 1)
        {
            // Lấy danh sách ParentName không trùng lặp
            var parentNames = _context.DanhMucs
                                      .Select(d => d.ParentId)
                                      .Distinct()
                                      .ToList();

            // Lọc danh sách con theo ParentName và tìm kiếm
            var query = _context.DanhMucs.AsQueryable();

            if (!string.IsNullOrEmpty(selectedParentName))
            {
                query = query.Where(d => d.ParentId == selectedParentName);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.Tendanhmuc.Contains(searchTerm));
            }

            // Phân trang
            int pageSize = 10;
            int totalItems = query.Count();
            var danhMucs = query
                            .OrderBy(d => d.ParentId) // Sắp xếp theo ParentName
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            var model = new DanhMucViewModel
            {
                ParentNames = parentNames,
                DanhMucs = danhMucs,
                SelectedParentName = selectedParentName,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult ThongKeDoanhThu()
        {
            try
            {
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var startOfMonth = new DateTime(today.Year, today.Month, 1);

                // Doanh thu hôm nay
                decimal revenueToday = (from ctdh in _context.ChiTietDonHangs
                                        join dh in _context.DonHangs on ctdh.Madon equals dh.Madon
                                        where ctdh.Status == 1 && dh.Ngaydat >= today && dh.Ngaydat < today.AddDays(1)
                                        select (decimal?)(ctdh.Soluong * ctdh.Gia)).Sum() ?? 0;

                // Doanh thu tuần này
                decimal revenueWeek = (from ctdh in _context.ChiTietDonHangs
                                       join dh in _context.DonHangs on ctdh.Madon equals dh.Madon
                                       where ctdh.Status == 1 && dh.Ngaydat >= startOfWeek && dh.Ngaydat < today.AddDays(1)
                                       select (decimal?)(ctdh.Soluong * ctdh.Gia)).Sum() ?? 0;

                // Doanh thu tháng này
                decimal revenueMonth = (from ctdh in _context.ChiTietDonHangs
                                        join dh in _context.DonHangs on ctdh.Madon equals dh.Madon
                                        where ctdh.Status == 1 && dh.Ngaydat >= startOfMonth && dh.Ngaydat < today.AddDays(1)
                                        select (decimal?)(ctdh.Soluong * ctdh.Gia)).Sum() ?? 0;

                return Json(new { revenueToday, revenueWeek, revenueMonth });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        public decimal ThongKeSL()
        {
            decimal TongDoanhThu = _context.ChiTietDonHangs
                .Where(m => m.Status == 1)
                .Sum(
                n => (n.Soluong)
            ).Value;
            return TongDoanhThu;
        }

        public double ThongKeDonHang()
        {
            double slddh = _context.DonHangs.Count();
            return slddh;
        }
        public double ThongKeKhachHang()
        {
            double slkh = _context.KhachHangs.Count();
            return slkh;
        }

        [Authorize(Roles = "Admin")]
        // GET: Admin/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.IdDanhmucNavigation)
                .Include(s => s.IdthuvienNavigation)
                .FirstOrDefaultAsync(m => m.Masp == id);
            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }

        // GET: Admin/Create
        public IActionResult Create()
        {
            showDropList();
            return View();
        }

        // POST: Admin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Masp,Idthuvien,IdDanhmuc,Tensp,Hinh,Giaban,Ngaycapnhat,Soluongton,Mota,Giamgia,Giakhuyenmai")] SanPham sanPham, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                if (files != null && files.Count > 0)
                {
                    // code for handling uploaded image file(s)
                    var filePaths = new List<string>();
                    string tempname = "";
                    foreach (var formFile in files)
                    {
                        if (formFile.Length > 0)
                        {
                            var fileName = Path.GetFileName(formFile.FileName);
                            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "Content/uploads", fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await formFile.CopyToAsync(stream);
                            }
                            tempname = fileName;
                            filePaths.Add(fileName);
                        }
                    }
                    sanPham.Hinh = "/Content/uploads/" + tempname;


                }

                var x = sanPham.Giaban;
                var y = sanPham.Giamgia;

                var z = (x * y) / 100;

                var price = x - z;

                sanPham.Giakhuyenmai = price;

                _context.Add(sanPham);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            showDropList();
            return View(sanPham);
        }

        // GET: Admin/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham == null)
            {
                return NotFound();
            }

            showDropList();

            return View(sanPham);
        }

        // POST: Admin/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Masp,IdDanhmuc,Idthuvien,Tensp,Hinh,Giaban,Ngaycapnhat,Soluongton,Mota,Giamgia,Giakhuyenmai")] SanPham sanPham, List<IFormFile> files, IFormCollection form)
        {
            if (id != sanPham.Masp)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {

                    if (files != null && files.Count > 0)
                    {
                        // code for handling uploaded image file(s)
                        var filePaths = new List<string>();
                        string tempname = "";
                        foreach (var formFile in files)
                        {
                            if (formFile.Length > 0)
                            {
                                var fileName = Path.GetFileName(formFile.FileName);
                                var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "Content/uploads", fileName);
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await formFile.CopyToAsync(stream);
                                }
                                tempname = fileName;
                                filePaths.Add(fileName);
                            }

                            //sanPham.Hinh = "/Conent/uploads/" + form["PathHinh"];
                        }
                    }
                    sanPham.Hinh = form["PathHinh"];

                    var x = sanPham.Giaban;
                    var y = sanPham.Giamgia;

                    var z = (x * y) / 100;

                    var price = x - z;

                    sanPham.Giakhuyenmai = price;


                    _context.Update(sanPham);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SanPhamExists(sanPham.Masp))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdDanhmuc"] = new SelectList(_context.DanhMucs, "IdDanhmuc", "IdDanhmuc", sanPham.IdDanhmuc);
            ViewData["Idthuvien"] = new SelectList(_context.ThuVienAnhs, "Idthuvien", "Idthuvien", sanPham.Idthuvien);
            return View(sanPham);
        }

        // GET: Admin/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.IdDanhmucNavigation)
                .Include(s => s.IdthuvienNavigation)
                .FirstOrDefaultAsync(m => m.Masp == id);
            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }

        // POST: Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sanPham = await _context.SanPhams.FindAsync(id);
            _context.SanPhams.Remove(sanPham);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SanPhamExists(int id)
        {
            return _context.SanPhams.Any(e => e.Masp == id);
        }

        //---------------------------------------Quản lý đơn hàng------------------------------------

        public async Task<IActionResult> QLDonHang(int? pageNumber)
        {
            const int pageSize = 5;

            var donHang = from dh in _context.DonHangs
                          join kh in _context.KhachHangs on dh.Makh equals kh.Makh
                          select dh;
            var paginatedProducts = await PaginatedList<DonHang>.CreateAsync(donHang, pageNumber ?? 1, pageSize);
            return View(paginatedProducts);
        }

        [HttpGet]
        public IActionResult QLChiTietDonHang(int id)
        {
            var donhang = _context.DonHangs.First(m => m.Madon == id);
            return View(donhang);
        }

        [HttpPost]
        public IActionResult QLChiTietDonHang(int id, IFormCollection collection)
        {
            var danhmuc = _context.DonHangs.First(m => m.Madon == id);
            var E_tendanhmuc = collection["giaohang"];
            var ngaygiao = string.Format("{0:MM/dd/yyyy}", collection["NgayGiao"]);
            danhmuc.Madon = id;
            if (string.IsNullOrEmpty(E_tendanhmuc))
            {
                TempData["Errnull"] = "Du lieu khong duoc de trong!";
            }
            else
            {
                danhmuc.Giaohang = E_tendanhmuc;
                danhmuc.Ngaygiao = DateTime.Parse(ngaygiao);
                _context.Update(danhmuc);

                if (E_tendanhmuc == "giao thành công")
                {
                    var ctdh = _context.ChiTietDonHangs.Where(m => m.Madon == id).ToList();
                    foreach (var item in ctdh)
                    {
                        item.Status = 1;
                        _context.Update(item);
                    }
                }
                else
                {
                    var ctdh = _context.ChiTietDonHangs.Where(m => m.Madon == id).ToList();
                    foreach (var item in ctdh)
                    {
                        item.Status = 0;
                        _context.Update(item);
                    }
                }

                _context.SaveChanges();
                return RedirectToAction("QLDonHang");
            }
            return View(danhmuc);
        }

        public async Task<IActionResult> ListKhachHang(int? pageNumber, string searchTerm)
        {
            const int pageSize = 10;
            var query = _context.KhachHangs.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(kh => kh.Tendangnhap.Contains(searchTerm) || kh.Hoten.Contains(searchTerm) || kh.Dienthoai.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            var paginatedCustomers = await PaginatedList<KhachHang>.CreateAsync(query.OrderBy(kh => kh.Makh), pageNumber ?? 1, pageSize);
            return View(paginatedCustomers);
        }

        public async Task<IActionResult> Dichvu(int? pageNumber, string searchTerm, DateTime? ngaydat)
        {
            const int pageSize = 10;
            var query = _context.DichVus.AsQueryable();

            // Tìm kiếm theo tên khách hàng hoặc tên dịch vụ
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(kh => kh.Hoten.Contains(searchTerm) || kh.Tendichvu.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            // Tìm kiếm theo ngày đặt
            if (ngaydat.HasValue)
            {
                query = query.Where(kh => kh.Ngaydat == ngaydat.Value.Date);
                ViewBag.NgayDat = ngaydat.Value.ToString("yyyy-MM-dd"); // Định dạng lại ngày cho View
            }

            var paginatedCustomers = await PaginatedList<DichVu>.CreateAsync(query.OrderBy(kh => kh.Iddichvu), pageNumber ?? 1, pageSize);
            return View(paginatedCustomers);
        }

        public IActionResult Editkh(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var kh = _context.KhachHangs.Find(id);
            if (kh == null)
            {
                return NotFound();
            }

            return View(kh);
        }

        [HttpPost]
        public async Task<IActionResult> Editkh(string id, [Bind("Makh,Hoten,Tendangnhap,Email,Diachi,Dienthoai,Ngaysinh,Status")] KhachHang khachHang)
        {
            if (id != khachHang.Makh)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(khachHang);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.KhachHangs.Any(e => e.Makh == khachHang.Makh))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction("ListKhachHang","Admin", 1);

        }

    }
}
