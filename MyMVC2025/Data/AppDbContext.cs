using Microsoft.EntityFrameworkCore;


    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }  // Define a table
    }

    public class Product  // Entity Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
