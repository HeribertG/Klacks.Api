using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Authentification;
using Klacks.Api.Models.CalendarSelections;
using Klacks.Api.Models.Histories;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Models.Settings;
using Klacks.Api.Models.Staffs;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Security.Claims;

namespace Klacks.Api.Datas;

public class DataBaseContext : IdentityDbContext
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public DataBaseContext(DbContextOptions<DataBaseContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options) =>
        this.httpContextAccessor = httpContextAccessor;

    public DbSet<Absence> Absence { get; set; } = default!;

    public DbSet<Address> Address { get; set; } = default!;

    public DbSet<Annotation> Annotation { get; set; } = default!;

    public virtual DbSet<AppUser> AppUser { get; set; } = default!;

    public DbSet<Break> Break { get; set; } = default!;

    public DbSet<BreakReason> BreakReason { get; set; } = default!;

    public DbSet<CalendarRule> CalendarRule { get; set; } = default!;

    public DbSet<CalendarSelection> CalendarSelection { get; set; } = default!;

    public DbSet<Client> Client { get; set; } = default!;

    public DbSet<ClientScheduleDetail> ClientScheduleDetail { get; set; } = default!;

    public DbSet<Communication> Communication { get; set; } = default!;

    public DbSet<CommunicationType> CommunicationType { get; set; } = default!;

    public DbSet<Countries> Countries { get; set; } = default!;

    public DbSet<Group> Group { get; set; } = default!;

    public DbSet<GroupItem> GroupItem { get; set; } = default!;

    public DbSet<History> History { get; set; } = default!;

    public DbSet<Macro> Macro { get; set; } = default!;

    public DbSet<MacroType> MacroType { get; set; } = default!;

    public DbSet<Membership> Membership { get; set; } = default!;

    public DbSet<PostcodeCH> PostcodeCH { get; set; } = default!;

    public virtual DbSet<RefreshToken> RefreshToken { get; set; } = default!;

    public DbSet<SelectedCalendar> SelectedCalendar { get; set; } = default!;

    public DbSet<Models.Settings.Settings> Settings { get; set; } = default!;

    public DbSet<Shift> Shift { get; set; } = default!;

    public DbSet<State> State { get; set; } = default!;

    public DbSet<Vat> Vat { get; set; } = default!;

    public DbSet<Work> Work { get; set; } = default!;

    public DbSet<AssignedGroup> AssignedGroup { get; set; } = default!;

    public DbSet<GroupVisibility> GroupVisibility { get; set; } = default!;

    public override int SaveChanges()
    {
        OnBeforeSaving();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        OnBeforeSaving();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        OnBeforeSaving();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaving();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
                 optionsBuilder.UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<int>("client_idnumber_seq", schema: "public")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.Property(e => e.IdNumber)
                  .HasDefaultValueSql("nextval('public.client_idnumber_seq')");

            entity.HasIndex(p => new { p.FirstName, p.SecondName, p.Name, p.MaidenName, p.Company, p.Gender, p.Type, p.LegalEntity, p.IsDeleted });
        });
        modelBuilder.Entity<Address>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Communication>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Membership>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Annotation>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<History>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Macro>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Absence>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Break>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<BreakReason>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Countries>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<State>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<SelectedCalendar>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<CalendarSelection>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Group>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<GroupItem>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Shift>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Work>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<AssignedGroup>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<GroupVisibility>().HasQueryFilter(p => !p.IsDeleted);


        modelBuilder.Entity<Address>().HasIndex(p => new { p.ClientId, p.Street, p.Street2, p.Street3, p.City, p.IsDeleted });
        modelBuilder.Entity<Communication>().HasIndex(p => new { p.Value, p.IsDeleted });
        modelBuilder.Entity<Annotation>().HasIndex(p => new { p.Note, p.IsDeleted });
        modelBuilder.Entity<History>().HasIndex(p => new { p.IsDeleted });
        modelBuilder.Entity<Macro>().HasIndex(p => new { p.IsDeleted, p.Name });
        modelBuilder.Entity<Absence>().HasIndex(p => new { p.IsDeleted });
        modelBuilder.Entity<Break>().HasIndex(p => new { p.IsDeleted, p.AbsenceId, p.ClientId });
        modelBuilder.Entity<BreakReason>().HasIndex(p => new { p.IsDeleted, p.Name });
        modelBuilder.Entity<CalendarRule>().HasIndex(p => new { p.State, p.Country });
        modelBuilder.Entity<SelectedCalendar>().HasIndex(p => new { p.State, p.Country, p.CalendarSelectionId });
        modelBuilder.Entity<Group>().HasIndex(p => new { p.Name });
        modelBuilder.Entity<GroupItem>().HasIndex(p => new { p.ClientId, p.GroupId });
        modelBuilder.Entity<Work>().HasIndex(p => new { p.ClientId, p.ShiftId });
        modelBuilder.Entity<Shift>().HasIndex(p => new { p.MacroId });
        modelBuilder.Entity<ClientScheduleDetail>().HasIndex(p => new { p.ClientId, p.CurrentYear, p.CurrentMonth });
        modelBuilder.Entity<AssignedGroup>().HasIndex(p => new { p.ClientId, p.GroupId });
        modelBuilder.Entity<GroupVisibility>().HasIndex(p => new { p.ClientId, p.GroupId });


        modelBuilder.Entity<Membership>()
       .HasOne(m => m.Client)
       .WithOne(c => c.Membership)
       .HasForeignKey<Membership>(m => m.ClientId)
       .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Client>()
        .HasMany(c => c.Addresses)
        .WithOne(a => a.Client)
        .HasForeignKey(a => a.ClientId)
        .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Client>()
       .HasMany(c => c.Communications)
       .WithOne(a => a.Client)
       .HasForeignKey(a => a.ClientId)
       .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Client>()
       .HasMany(c => c.Annotations)
       .WithOne(a => a.Client)
       .HasForeignKey(a => a.ClientId)
       .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Client>()
       .HasMany(c => c.Works)
       .WithOne(a => a.Client)
       .HasForeignKey(a => a.ClientId)
       .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Client>()
      .HasMany(c => c.GroupVisibilities)
      .WithOne(a => a.Client)
      .HasForeignKey(a => a.ClientId)
      .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Client>()
     .HasMany(c => c.GroupItems)
     .WithOne(a => a.Client)
     .HasForeignKey(a => a.ClientId)
     .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<SelectedCalendar>()
        .HasOne(p => p.CalendarSelection)
        .WithMany(b => b.SelectedCalendars)
        .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Group>()
       .HasMany(g => g.GroupItems)
       .WithOne(gi => gi.Group)
       .HasForeignKey(gi => gi.GroupId)
       .OnDelete(DeleteBehavior.Cascade);



        modelBuilder.Entity<Work>()
               .HasOne(p => p.Client)
               .WithMany(b => b.Works)
               .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Work>()
            .HasOne(p => p.Shift);

        modelBuilder.Entity<GroupVisibility>()
              .HasOne(p => p.Client)
              .WithMany(b => b.GroupVisibilities)
              .OnDelete(DeleteBehavior.Cascade);

    }

    private void OnBeforeSaving()
    {
        var entries = ChangeTracker.Entries();
        foreach (var entry in entries)
        {
            if (entry.Entity is not BaseEntity entityBase)
            {
                continue;
            }

            var now = DateTime.UtcNow;
            const string defaultUser = "Annonymus";
            string currentUserName = defaultUser;

            try
            {
                var claimValue = httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(claimValue))
                {
                    currentUserName = claimValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Abrufen des Benutzers für die Auditierung: {ex.ToString()}");
            }

            switch (entry.State)
            {
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entityBase.DeletedTime = now;
                    entityBase.IsDeleted = true;
                    entityBase.CurrentUserDeleted = currentUserName!;
                    break;

                case EntityState.Added:
                    entityBase.CreateTime = now;
                    entityBase.CurrentUserCreated = currentUserName!;

                    break;

                case EntityState.Modified:
                    entityBase.UpdateTime = now;
                    entityBase.CurrentUserUpdated = currentUserName!;

                    break;
            }
        }
    }

}
