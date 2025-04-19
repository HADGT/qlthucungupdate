using System.Collections.Generic;
using System;

namespace qlthucung.Models
{
    public class LichDatViewModel
    {
        public DateTime Ngay { get; set; }
        public int SoLuong { get; set; }
        public List<DichVu> Dv { get; set; }
    }
}
