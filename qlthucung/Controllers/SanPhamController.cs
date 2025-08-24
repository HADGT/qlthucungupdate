using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using qlthucung.Models;

namespace qlthucung.Controllers
{
    public class SanPhamController : Controller
    {
        private readonly AppDbContext _context;

        public SanPhamController(AppDbContext context)
        {
            _context = context;
        }

        // GET: All SanPham
        public async Task<IActionResult> Index(int Id)
        {
            var model = new HomeVm();

            // Sản phẩm nổi bật
            model.SanPhamNoiBat = await getSPNoiBat();

            // Sản phẩm chung (12 sản phẩm mới nhất)
            model.AllProducts = await _context.SanPhams
                .OrderByDescending(sp => sp.Masp)
                .Take(12)
                .ToListAsync();

            // Root categories L1
            model.RootCategoriesl1 = await _context.DanhMucs
                .Where(dm => dm.ParentID == null)
                .Select(dm => new CategoryVm { Id = dm.IdDanhmuc, Ten = dm.Tendanhmuc })
                .ToListAsync();

            // Nếu chưa chọn thì mặc định lấy thằng đầu tiên
            if (Id == null)
            {
                Id = model.RootCategoriesl1.First().Id;
            }

            // Lấy Level 2 theo Tên L1 đã chọn
            model.RootCategoriesl2 = await GetMenulev2(Id);

            // Lấy sản phẩm cho từng L2
            foreach (var cat in model.RootCategoriesl2)
            {
                model.ProductsByRoot[cat.Id] = await GetProductsByRootName(cat.Id);
            }

            return View(model);
        }

        /// <summary>
        /// API Ajax: lấy L2 + sản phẩm theo tên L1
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLevel2AndProducts(int l1Id)
        {
            var l2s = await GetMenulev2(l1Id);

            var productsByL2 = new Dictionary<int, List<SanPham>>();
            foreach (var cat in l2s)
            {
                productsByL2[cat.Id] = await GetProductsByRootName(cat.Id);
            }

            return Json(new { level2 = l2s, products = productsByL2 });
        }

        /// <summary>
        /// Lấy menu L2 theo tên L1
        /// </summary>
        private async Task<List<CategoryVm>> GetMenulev2(int l1id)
        {
            var catName = await _context.DanhMucs
                .Where(dm => dm.IdDanhmuc == l1id)
                .Select(dm => dm.Tendanhmuc)
                .FirstOrDefaultAsync();

            return await _context.DanhMucs
                .Where(dm => dm.ParentID == catName) // ParentID lưu tên cha
                .Select(dm => new CategoryVm
                {
                    Id = dm.IdDanhmuc,
                    Ten = dm.Tendanhmuc
                })
                .ToListAsync();
        }

        /// <summary>
        /// Lấy sản phẩm theo tên danh mục
        /// </summary>
        private async Task<List<SanPham>> GetProductsByRootName(int Id)
        {
            // Lấy sản phẩm theo IdDanhmuc
            return await _context.SanPhams
                .Where(sp => sp.IdDanhmuc == Id)
                .ToListAsync();
        }

        //cac ham lay ra san pham
        #region
        //lay san pham noi bat
        private async Task<List<SanPham>> getSPNoiBat()
        {
            return await _context.SanPhams
                .OrderByDescending(sp => sp.Masp)
                .Take(10)
                .ToListAsync();
        }
        #endregion

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var sanpham = await _context.SanPhams.FirstOrDefaultAsync(m => m.Masp == id);
            if (sanpham == null)
            {
                return NotFound();
            }

            //random san pham
            List<SanPham> products = _context.SanPhams.OrderBy(x => Guid.NewGuid()).Skip(5).Take(5).ToList();
            ViewBag.getSPRanDom = products;

            getThuVienAnhList(id);

            return View(sanpham);
        }

        private void getThuVienAnhList(int? id)
        {

            //sp vs thu vien anh
            List<SanPham> sanpham = _context.SanPhams.ToList();
            List<ThuVienAnh> thuvienanh = _context.ThuVienAnhs.ToList();
            var thu = from sp in sanpham
                      join tv in thuvienanh
                              on sp.Idthuvien equals tv.Idthuvien
                      where (sp.Masp == id && sp.Idthuvien == tv.Idthuvien)
                      select new ViewModel
                      {
                          sanpham = sp,
                          thuvienanh = tv
                      };

            ViewBag.getthuvienanh = thu;

        }

        [HttpGet]
        public async Task<IActionResult> Search(string search)
        {
            var searchProduct = from m in _context.SanPhams
                                select m;

            if (!String.IsNullOrEmpty(search))
            {
                searchProduct = searchProduct.Where(s => s.Tensp.Contains(search));
                if (!searchProduct.Any())
                {
                    TempData["nameProduct"] = search;
                    return RedirectToAction("NotFoundProduct", "SanPham");
                }
            }
            else
            {
                return RedirectToAction("NotFoundProduct", "SanPham");
            }

            TempData["nameProduct"] = search;
            return View(await searchProduct.ToListAsync());
        }

        public IActionResult NotFoundProduct()
        {
            return View();
        }

        public async Task<IActionResult> TatCaSanPham(int? pageNumber)
        {
            const int pageSize = 5;

            var products = _context.SanPhams.AsNoTracking();
            var paginatedProducts = await PaginatedList<SanPham>.CreateAsync(products, pageNumber ?? 1, pageSize);

            return View(paginatedProducts);
        }


    }
}
