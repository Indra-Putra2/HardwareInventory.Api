using HardwareInventoryLibrary.Brand;
using HardwareInventoryLibrary.Ram;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<RamDatabaseModel> RamDatas => Set<RamDatabaseModel>();
    public DbSet<BrandDatabaseModel> BrandDatas => Set<BrandDatabaseModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RamDatabaseModel>()
           .HasOne<BrandDatabaseModel>()
           .WithMany()
           .HasForeignKey(r => r.BrandId)
           .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BrandDatabaseModel>()
            .HasIndex(b => b.Name)
            .IsUnique();

        modelBuilder.Entity<RamDatabaseModel>()
            .Property(r => r.Price)
            .HasColumnType("REAL");

        modelBuilder.Entity<RamDatabaseModel>()
            .Property(r => r.PricePerGB)
            .HasColumnType("REAL");
    }
}