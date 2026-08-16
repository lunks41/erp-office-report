using Microsoft.EntityFrameworkCore;

namespace apireport.Extensions
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> dbContextOptions)
           : base(dbContextOptions)
        {
        }
    }
}