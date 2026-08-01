using Microsoft.EntityFrameworkCore;

namespace erpofficereport.Extensions
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> dbContextOptions)
           : base(dbContextOptions)
        {
        }
    }
}