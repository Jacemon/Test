using LinkShorter.Models;

namespace LinkShorter.Data;

using Microsoft.EntityFrameworkCore;

public class AppDbContext(
    DbContextOptions<AppDbContext> options
) : DbContext(options)
{
    public DbSet<ShortUrl> Urls { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShortUrl>(entity =>
        { 
            entity.HasIndex(e => e.ShortCode).IsUnique();
            
            entity.Property(e => e.ShortCode).HasMaxLength(10);
            entity.Property(e => e.LongUrl).IsRequired();
        });
    }
}
