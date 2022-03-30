using LoginApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<UserModel> Users { get; set; } = default!;
        public DbSet<UserToken> UserTokens { get; set; } = default!;
        public DbSet<ResetToken> ResetTokens { get; set; } = default!;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
    }
}
