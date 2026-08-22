using Microsoft.AspNetCore.Identity;

namespace AuthenticationDemo.Models {
    public class Users : IdentityUser{
        public string FullName { get; set; }
        public string? EmailConfirmationCode { get; set; }
        public DateTime? EmailConfirmationCodeExpiry { get; set; }
    }
}
