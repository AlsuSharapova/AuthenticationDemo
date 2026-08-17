using Microsoft.AspNetCore.Identity;

namespace AuthenticationDemo.Models {
    public class Users : IdentityUser{
        public string FullName { get; set; }
    }
}
