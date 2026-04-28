using Microsoft.EntityFrameworkCore;
using CertificateApp.Models;

namespace CertificateApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<CertificateOfAttendance> Certificates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CertificateOfAttendance>(entity =>
        {
            entity.ToTable("CertificateOfAttendance");
            entity.HasKey(e => e.StudentID);
            entity.Property(e => e.StudentID).ValueGeneratedNever();
            entity.Property(e => e.BornDate).HasColumnType("date");
            entity.Property(e => e.StudiedFrom).HasColumnType("date");
            entity.Property(e => e.StudiedTo).HasColumnType("date");
        });
    }
}
