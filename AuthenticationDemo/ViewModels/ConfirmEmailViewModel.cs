using System.ComponentModel.DataAnnotations;

namespace AuthenticationDemo.ViewModels {
    public class ConfirmEmailViewModel {
        [Required]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Please enter the confirmation code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The code must be 6 digits.")]
        public string Code { get; set; }

        public int SecondsRemaining { get; set; }
    }
}
