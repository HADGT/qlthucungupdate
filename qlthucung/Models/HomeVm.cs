using System.Collections.Generic;

namespace qlthucung.Models
{
    public class HomeVm
    {
        public List<CategoryVm> RootCategoriesl1 { get; set; } = new List<CategoryVm>();
        public List<CategoryVm> RootCategoriesl2 { get; set; } = new List<CategoryVm>();

        public Dictionary<int, List<SanPham>> ProductsByRoot { get; set; }
            = new Dictionary<int, List<SanPham>>();

        // Thêm list sản phẩm chung
        public List<SanPham> AllProducts { get; set; } = new List<SanPham>();

        // Nếu muốn có sản phẩm nổi bật
        public List<SanPham> SanPhamNoiBat { get; set; } = new List<SanPham>();

        public int SelectedL1 { get; set; }
    }
}
