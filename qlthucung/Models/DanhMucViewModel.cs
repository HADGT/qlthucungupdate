using System.Collections.Generic;

namespace qlthucung.Models
{
    public class DanhMucViewModel
    {
        public IEnumerable<string> ParentNames { get; set; }
        public IEnumerable<DanhMuc> DanhMucs { get; set; }
        public string SelectedParentName { get; set; }
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

}
