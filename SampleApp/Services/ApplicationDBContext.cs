using Microsoft.EntityFrameworkCore;
using SampleApp.Models;

namespace SampleApp.Services
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions options) : base(options)
        {
            
        }
        public DbSet<Registration> Registation { get; set; }

        public DbSet<PasswordResetRequest> ResetTokens { get; set; }
    }
}
