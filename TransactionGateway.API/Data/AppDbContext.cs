using Microsoft.EntityFrameworkCore;
using TransactionGateway.API.Models;

namespace TransactionGateway.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Transaction> Transactions => Set<Transaction>();
    }
}
