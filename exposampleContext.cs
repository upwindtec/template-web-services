//
// Template Web Services Application for Upwindtec Cloud.
// Can be freely adapted and distributed without resitrictions.
// For more information, visit https://www.upwindtec.pt
//
using Microsoft.EntityFrameworkCore;

namespace expo_sample_web_services;

public partial class ExpoSampleContext : DbContext
{
    public ExpoSampleContext(DbContextOptions<ExpoSampleContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Item> items { get; set; }

    public static PostgresNotificationHandler notificationHandler = new PostgresNotificationHandler();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("items_pkey");

            entity.Property(e => e.done).HasDefaultValue(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public static Dictionary<string, Type> entityTypes = new()
    {
        { "items", typeof(Item) }
    };
}
