using System.ComponentModel.DataAnnotations;

namespace AuthenticationDemo.ViewModels {
    public class VerifyEmailViewModel {

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }
    }
}
