using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer("Data Source=MAZENKHATTAB\\SQLEXPRESS01;Initial Catalog=Rock;Integrated Security=True;Encrypt=False;Trust Server Certificate=True");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
