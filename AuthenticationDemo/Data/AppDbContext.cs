using AuthenticationDemo.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationDemo.Data {
    public class AppDbContext : IdentityDbContext<Users> {
        public AppDbContext(DbContextOptions options): base(options) {
            
        }
    }
}
