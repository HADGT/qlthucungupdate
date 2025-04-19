using System.ComponentModel.DataAnnotations;

namespace qlthucung.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string UserName { get; set; }
    }
}
