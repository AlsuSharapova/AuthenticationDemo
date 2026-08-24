using System.ComponentModel.DataAnnotations;

namespace AuthenticationDemo.ViewModels {
    public class ConfirmEmailViewModel {
        [Required]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Enter the confirmation code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 characters long.")]
        public string Code { get; set; }
    }
}
