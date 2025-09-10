using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Histories;
using Klacks.Api.Domain.Models.LLM;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Security.Claims;

namespace Klacks.Api.Infrastructure.Persistence;

public class DataBaseContext : IdentityDbContext
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public DataBaseContext(DbContextOptions<DataBaseContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options) =>
        this.httpContextAccessor = httpContextAccessor;

    public DbSet<Absence> Absence { get; set; }  

    public DbSet<Address> Address { get; set; }  

    public DbSet<Annotation> Annotation { get; set; }  

    public virtual DbSet<AppUser> AppUser { get; set; }  

    public DbSet<Break> Break { get; set; }  

    public DbSet<BreakReason> BreakReason { get; set; }  

    public DbSet<CalendarRule> CalendarRule { get; set; }  

    public DbSet<CalendarSelection> CalendarSelection { get; set; }  

    public DbSet<Client> Client { get; set; }  

    public DbSet<ClientScheduleDetail> ClientScheduleDetail { get; set; }  

    public DbSet<Communication> Communication { get; set; }  

    public DbSet<CommunicationType> CommunicationType { get; set; }  

    public DbSet<Countries> Countries { get; set; }  

    public DbSet<Group> Group { get; set; }  

    public DbSet<GroupItem> GroupItem { get; set; }  

    public DbSet<History> History { get; set; }  

    public DbSet<Macro> Macro { get; set; }  

    public DbSet<MacroType> MacroType { get; set; }  

    public DbSet<Membership> Membership { get; set; }  

    public DbSet<PostcodeCH> PostcodeCH { get; set; }  

    public virtual DbSet<RefreshToken> RefreshToken { get; set; }  

    public DbSet<SelectedCalendar> SelectedCalendar { get; set; }  

    public DbSet<Settings> Settings { get; set; }  

    public DbSet<Shift> Shift { get; set; } 

    public DbSet<ShiftDayAssignment> ShiftDayAssignments { get; set; }

    public DbSet<State> State { get; set; }  

    public DbSet<Vat> Vat { get; set; }  

    public DbSet<Work> Work { get; set; }  

    public DbSet<AssignedGroup> AssignedGroup { get; set; }  

    public DbSet<GroupVisibility> GroupVisibility { get; set; }

    public DbSet<Contract> Contract { get; set; }

    // LLM DbSets
    public DbSet<LLMProvider> LLMProviders { get; set; }
    public DbSet<LLMModel> LLMModels { get; set; }
    public DbSet<LLMUsage> LLMUsages { get; set; }
    public DbSet<LLMConversation> LLMConversations { get; set; }
    public DbSet<LLMMessage> LLMMessages { get; set; }

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
        modelBuilder.Entity<ShiftDayAssignment>().HasNoKey();

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
        modelBuilder.Entity<Contract>().HasQueryFilter(p => !p.IsDeleted);
        
        // LLM Query Filters
        modelBuilder.Entity<LLMProvider>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<LLMModel>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<LLMUsage>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<LLMConversation>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<LLMMessage>().HasQueryFilter(p => !p.IsDeleted);

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
        modelBuilder.Entity<GroupItem>().HasIndex(p => new { p.ClientId, p.GroupId, p.ShiftId });
        modelBuilder.Entity<Work>().HasIndex(p => new { p.ClientId, p.ShiftId });
        modelBuilder.Entity<Shift>().HasIndex(p => new { p.MacroId, p.ClientId, p.Status , p.FromDate, p.UntilDate });
        modelBuilder.Entity<ClientScheduleDetail>().HasIndex(p => new { p.ClientId, p.CurrentYear, p.CurrentMonth });
        modelBuilder.Entity<AssignedGroup>().HasIndex(p => new { p.ClientId, p.GroupId });
        modelBuilder.Entity<GroupVisibility>().HasIndex(p => new { p.AppUserId, p.GroupId });
        modelBuilder.Entity<Contract>().HasIndex(p => new { p.Name, p.ValidFrom, p.ValidUntil });

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

        modelBuilder.Entity<Shift>()
        .HasOne(s => s.Client)
        .WithMany()
        .HasForeignKey(s => s.ClientId)
        .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GroupItem>()
         .HasOne(gi => gi.Shift)
         .WithMany()
         .HasForeignKey(gi => gi.ShiftId)
         .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Work>()
               .HasOne(p => p.Client)
               .WithMany(b => b.Works)
               .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Work>()
            .HasOne(p => p.Shift);

        modelBuilder.Entity<GroupVisibility>(entity =>
        {
          entity.Property(e => e.AppUserId)
                .HasColumnName("app_user_id");

          entity.HasIndex(p => new { p.AppUserId, p.GroupId });

          entity.HasOne(p => p.AppUser)
                .WithMany()
                .HasForeignKey(p => p.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Membership>()
           .HasOne(m => m.Contract)
           .WithMany()
           .HasForeignKey(m => m.ContractId)
           .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Contract>()
           .HasOne(c => c.CalendarSelection)
           .WithMany()
           .HasForeignKey(c => c.CalendarSelectionId)
           .OnDelete(DeleteBehavior.Restrict);
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
