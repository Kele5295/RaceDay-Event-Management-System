using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.ToTable("User", table =>
                table.HasCheckConstraint(
                    "CK_User_Role",
                    "[Role] IN ('Organiser', 'Participant')"));
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.HasIndex(t => t.TypeName).IsUnique();

            entity.HasData(
                new EventType
                {
                    EventTypeID = 1,
                    TypeName = "Road Running",
                    Description =
                        "Road-based running events over different distances."
                },
                new EventType
                {
                    EventTypeID = 2,
                    TypeName = "Walking",
                    Description =
                        "Organised walking events for recreational and competitive participants."
                },
                new EventType
                {
                    EventTypeID = 3,
                    TypeName = "Cycling",
                    Description =
                        "Road cycling events for individual participants."
                });
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Event", table =>
                table.HasCheckConstraint(
                    "CK_Event_Distance",
                    "[Distance] > 0"));

            entity.HasOne(e => e.Organiser)
                .WithMany()
                .HasForeignKey(e => e.OrganiserID)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.EventType)
                .WithMany()
                .HasForeignKey(e => e.EventTypeID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => new { c.EventID, c.CategoryName })
                .IsUnique();

            entity.ToTable("Category", table =>
                table.HasCheckConstraint(
                    "CK_Category_MaximumParticipants",
                    "[MaximumParticipants] IS NULL OR [MaximumParticipants] > 0"));

            entity.HasOne(c => c.Event)
                .WithMany()
                .HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasIndex(e => new { e.ParticipantID, e.EventID })
                .IsUnique();

            entity.Property(e => e.EnrolmentDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSDATETIME()");

            entity.Property(e => e.Status)
                .HasDefaultValue("Confirmed");

            entity.ToTable("Enrolment", table =>
                table.HasCheckConstraint(
                    "CK_Enrolment_Status",
                    "[Status] IN ('Pending', 'Confirmed', 'Cancelled')"));

            entity.HasOne(e => e.Participant)
                .WithMany()
                .HasForeignKey(e => e.ParticipantID)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Event)
                .WithMany()
                .HasForeignKey(e => e.EventID)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasIndex(r => r.EnrolmentID).IsUnique();

            entity.Property(r => r.RecordedDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSDATETIME()");

            entity.ToTable("Result", table =>
                table.HasCheckConstraint(
                    "CK_Result_FinishingPosition",
                    "[FinishingPosition] IS NULL OR [FinishingPosition] > 0"));

            entity.HasOne(r => r.Enrolment)
                .WithOne()
                .HasForeignKey<Result>(r => r.EnrolmentID)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}