using Microsoft.EntityFrameworkCore;
using WebAPI_image.Models.Domain;

namespace WebAPI_image.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Image> Images { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Image>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Images)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Mỗi user chỉ favorite 1 ảnh 1 lần
            modelBuilder.Entity<Favorite>()
                .HasIndex(f => new { f.Username, f.ImageId })
                .IsUnique();

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Image)
                .WithMany()
                .HasForeignKey(f => f.ImageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}