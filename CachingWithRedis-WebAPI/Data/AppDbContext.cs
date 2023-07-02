using CachingWithRedis_WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CachingWithRedis_WebAPI.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Driver> Drivers { get; set; }
        
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}