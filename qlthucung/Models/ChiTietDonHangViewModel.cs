using System.Collections.Generic;

namespace qlthucung.Models
{
    public class ChiTietDonHangViewModel
    {
        public DonHang DonHang { get; set; }
        public List<ChiTietDonHangItemViewModel> ChiTietDonHangs { get; set; }
        public KhachHang KhachHang { get; set; }
        public MoMoPayment? GiaoDich { get; set; }
    }
}
