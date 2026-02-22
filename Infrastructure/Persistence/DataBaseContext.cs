// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Histories;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence.Converters;
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

    public DbSet<BreakPlaceholder> BreakPlaceholder { get; set; }  

    public DbSet<AbsenceDetail> AbsenceDetail { get; set; }  

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

    public DbSet<Membership> Membership { get; set; }  

    public DbSet<PostcodeCH> PostcodeCH { get; set; }  

    public virtual DbSet<RefreshToken> RefreshToken { get; set; }  

    public DbSet<SelectedCalendar> SelectedCalendar { get; set; }  

    public DbSet<Settings> Settings { get; set; }

    public DbSet<Branch> Branch { get; set; }

    public DbSet<Shift> Shift { get; set; }

    public DbSet<ShiftDayAssignment> ShiftDayAssignments { get; set; }

    public DbSet<ScheduleCell> ScheduleCells { get; set; }

    public DbSet<ContainerTemplate> ContainerTemplate { get; set; }

    public DbSet<ContainerTemplateItem> ContainerTemplateItem { get; set; }

    public DbSet<State> State { get; set; }

    public DbSet<Work> Work { get; set; }

    public DbSet<Break> Break { get; set; }

    public DbSet<WorkChange> WorkChange { get; set; }

    public DbSet<Expenses> Expenses { get; set; }

    public DbSet<ShiftExpenses> ShiftExpenses { get; set; }

    public DbSet<ScheduleChange> ScheduleChange { get; set; }

    public DbSet<ClientPeriodHours> ClientPeriodHours { get; set; }

    public DbSet<IndividualPeriod> IndividualPeriod { get; set; }

    public DbSet<Period> Period { get; set; }

    public DbSet<AssignedGroup> AssignedGroup { get; set; }  

    public DbSet<GroupVisibility> GroupVisibility { get; set; }

    public DbSet<Contract> Contract { get; set; }

    public DbSet<ClientContract> ClientContract { get; set; }

    public DbSet<ClientImage> ClientImage { get; set; }

    // LLM DbSets
    public DbSet<LLMProvider> LLMProviders { get; set; }

    public DbSet<LLMModel> LLMModels { get; set; }

    public DbSet<LLMUsage> LLMUsages { get; set; }

    public DbSet<LLMConversation> LLMConversations { get; set; }

    public DbSet<LLMMessage> LLMMessages { get; set; }

    // Skill DbSets
    public DbSet<SkillUsageRecord> SkillUsageRecords { get; set; }

    // Identity Provider DbSets
    public DbSet<IdentityProvider> IdentityProviders { get; set; }

    public DbSet<IdentityProviderSyncLog> IdentityProviderSyncLogs { get; set; }

    // AI DbSets (Legacy - kept for migration compatibility)
    public DbSet<AiMemory> AiMemories { get; set; }
    public DbSet<AiSoul> AiSouls { get; set; }
    public DbSet<AiGuidelines> AiGuidelines { get; set; }
    public DbSet<LlmFunctionDefinition> LlmFunctionDefinitions { get; set; }
    public DbSet<HeartbeatConfig> HeartbeatConfigs { get; set; }

    // Agent Architecture DbSets
    public DbSet<Agent> Agents { get; set; }
    public DbSet<AgentTemplate> AgentTemplates { get; set; }
    public DbSet<AgentLink> AgentLinks { get; set; }
    public DbSet<AgentSoulSection> AgentSoulSections { get; set; }
    public DbSet<AgentSoulHistory> AgentSoulHistories { get; set; }
    public DbSet<AgentMemory> AgentMemories { get; set; }
    public DbSet<AgentMemoryTag> AgentMemoryTags { get; set; }
    public DbSet<AgentSession> AgentSessions { get; set; }
    public DbSet<AgentSessionMessage> AgentSessionMessages { get; set; }
    public DbSet<AgentSkill> AgentSkills { get; set; }
    public DbSet<AgentSkillExecution> AgentSkillExecutions { get; set; }

    // Scheduling DbSets
    public DbSet<SchedulingRule> SchedulingRules { get; set; }

    // Report DbSets
    public DbSet<ReportTemplate> ReportTemplates { get; set; }

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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShiftDayAssignment>().HasNoKey();
        modelBuilder.Entity<ScheduleCell>().HasNoKey().ToView(null);

        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<int>("client_idnumber_seq", schema: "public")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.Property(e => e.IdNumber)
                  .HasDefaultValueSql("nextval('public.client_idnumber_seq')");

            entity.HasIndex(p => new { p.IsDeleted, p.Name, p.FirstName });
            entity.HasIndex(p => new { p.IsDeleted, p.FirstName, p.Name });
            entity.HasIndex(p => new { p.IsDeleted, p.Company, p.Name });
            entity.HasIndex(p => new { p.IsDeleted, p.IdNumber });
            entity.HasIndex(p => new { p.FirstName, p.SecondName, p.Name, p.MaidenName, p.Company, p.Gender, p.Type, p.LegalEntity, p.IsDeleted });
        });
        modelBuilder.Entity<Address>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Communication>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Membership>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Annotation>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<History>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Macro>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.OwnsOne(m => m.Description, nav => nav.ToJson("description"));
        });
        modelBuilder.Entity<Absence>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.OwnsOne(a => a.Name, nav => nav.ToJson("name"));
            entity.OwnsOne(a => a.Description, nav => nav.ToJson("description"));
            entity.OwnsOne(a => a.Abbreviation, nav => nav.ToJson("abbreviation"));
        });
        modelBuilder.Entity<BreakPlaceholder>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<AbsenceDetail>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.OwnsOne(a => a.DetailName, nav => nav.ToJson("detail_name"));
            entity.OwnsOne(a => a.Description, nav => nav.ToJson("description"));
        });
        modelBuilder.Entity<Branch>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Countries>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.OwnsOne(c => c.Name, nav => nav.ToJson("name"));
        });
        modelBuilder.Entity<State>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.OwnsOne(s => s.Name, nav => nav.ToJson("name"));
        });
        modelBuilder.Entity<SelectedCalendar>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<CalendarSelection>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Group>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<GroupItem>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Shift>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<ContainerTemplate>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.Property(e => e.RouteInfo).HasJsonbConversion<RouteInfo>();
        });
        modelBuilder.Entity<ContainerTemplateItem>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Work>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Break>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.OwnsOne(b => b.Description, nav =>
            {
                nav.ToJson("description");
            });
        });
        modelBuilder.Entity<WorkChange>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Expenses>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<ShiftExpenses>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<ScheduleChange>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.ClientId, p.ChangeDate }).IsUnique();
        });
        modelBuilder.Entity<AssignedGroup>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<GroupVisibility>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Contract>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<ClientContract>().HasQueryFilter(p => !p.IsDeleted);

        // LLM Query Filters
        modelBuilder.Entity<LLMProvider>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.Property(e => e.Settings).HasJsonbConversionWithComparer<Dictionary<string, object>>();
        });
        modelBuilder.Entity<LLMModel>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<LLMUsage>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<LLMConversation>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<LLMMessage>().HasQueryFilter(p => !p.IsDeleted);

        // Identity Provider Query Filters and Configuration
        modelBuilder.Entity<IdentityProvider>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.Property(e => e.AttributeMapping).HasJsonbConversionWithComparer<Dictionary<string, string>>();
            entity.HasIndex(p => new { p.IsDeleted, p.IsEnabled, p.SortOrder });
        });
        modelBuilder.Entity<IdentityProviderSyncLog>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.IdentityProviderId, p.ExternalId });
            entity.HasIndex(p => new { p.ClientId, p.IdentityProviderId });
        });

        modelBuilder.Entity<IdentityProviderSyncLog>()
            .HasOne(s => s.IdentityProvider)
            .WithMany()
            .HasForeignKey(s => s.IdentityProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IdentityProviderSyncLog>()
            .HasOne(s => s.Client)
            .WithMany()
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // AI Memory Configuration
        modelBuilder.Entity<AiMemory>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.IsDeleted, p.Category, p.Importance });
        });

        // AI Soul Configuration
        modelBuilder.Entity<AiSoul>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.IsDeleted, p.IsActive });
        });

        // AI Guidelines Configuration
        modelBuilder.Entity<AiGuidelines>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.IsDeleted, p.IsActive });
        });

        // LLM Function Definitions Configuration
        modelBuilder.Entity<LlmFunctionDefinition>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.IsDeleted, p.IsEnabled, p.SortOrder });
        });

        // Heartbeat Configuration
        modelBuilder.Entity<HeartbeatConfig>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.IsDeleted, p.UserId });
            entity.HasIndex(p => new { p.IsDeleted, p.IsEnabled });
        });

        // Report Templates Configuration
        modelBuilder.Entity<ReportTemplate>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.Property(e => e.PageSetup).HasJsonbConversion<ReportPageSetup>();
            entity.Property(e => e.Sections).HasJsonbListConversion<ReportSection>();
            entity.Property(e => e.DataSetIds).HasJsonbListConversion<string>();
            entity.HasIndex(p => new { p.IsDeleted, p.Type, p.Name });
        });

        modelBuilder.Entity<Address>().HasIndex(p => new { p.ClientId, p.Street, p.Street2, p.Street3, p.City, p.IsDeleted });
        modelBuilder.Entity<Communication>().HasIndex(p => new { p.Value, p.IsDeleted });
        modelBuilder.Entity<Annotation>().HasIndex(p => new { p.Note, p.IsDeleted });
        modelBuilder.Entity<Membership>().HasIndex(p => new { p.ClientId, p.ValidFrom, p.ValidUntil, p.IsDeleted });
        modelBuilder.Entity<History>().HasIndex(p => new { p.IsDeleted });
        modelBuilder.Entity<Macro>().HasIndex(p => new { p.IsDeleted, p.Name });
        modelBuilder.Entity<Absence>().HasIndex(p => new { p.IsDeleted });
        modelBuilder.Entity<BreakPlaceholder>().HasIndex(p => new { p.IsDeleted, p.AbsenceId, p.ClientId });
        modelBuilder.Entity<BreakPlaceholder>().HasIndex(p => new { p.IsDeleted, p.ClientId, p.From, p.Until });
        modelBuilder.Entity<AbsenceDetail>().HasIndex(p => new { p.IsDeleted, p.AbsenceId });
        modelBuilder.Entity<CalendarRule>(entity =>
        {
            entity.HasIndex(p => new { p.State, p.Country });
            entity.OwnsOne(c => c.Name, nav => nav.ToJson("name"));
            entity.OwnsOne(c => c.Description, nav => nav.ToJson("description"));
            entity.Navigation(c => c.Name).IsRequired();
            entity.Navigation(c => c.Description).IsRequired();
        });
        modelBuilder.Entity<SelectedCalendar>().HasIndex(p => new { p.State, p.Country, p.CalendarSelectionId });
        modelBuilder.Entity<Group>().HasIndex(p => new { p.Name });
        modelBuilder.Entity<GroupItem>().HasIndex(p => new { p.GroupId, p.ClientId, p.IsDeleted });
        modelBuilder.Entity<GroupItem>().HasIndex(p => new { p.ClientId, p.GroupId, p.ShiftId });
        modelBuilder.Entity<Work>().HasIndex(p => new { p.ClientId, p.ShiftId });
        modelBuilder.Entity<Break>().HasIndex(p => new { p.ClientId });
        modelBuilder.Entity<Shift>().HasIndex(p => new { p.MacroId, p.ClientId, p.Status , p.FromDate, p.UntilDate });
        modelBuilder.Entity<ContainerTemplate>().HasIndex(p => new { p.Id, p.ContainerId, p.Weekday, p.IsWeekdayAndHoliday, p.IsHoliday });
        modelBuilder.Entity<ClientScheduleDetail>().HasIndex(p => new { p.ClientId, p.CurrentYear, p.CurrentMonth });
        modelBuilder.Entity<AssignedGroup>().HasIndex(p => new { p.ClientId, p.GroupId });
        modelBuilder.Entity<GroupVisibility>().HasIndex(p => new { p.AppUserId, p.GroupId });
        modelBuilder.Entity<Contract>().HasIndex(p => new { p.Name, p.ValidFrom, p.ValidUntil });
        modelBuilder.Entity<ClientContract>().HasIndex(p => new { p.ClientId, p.ContractId, p.FromDate, p.UntilDate });
        modelBuilder.Entity<ClientPeriodHours>()
            .HasIndex(p => new { p.ClientId, p.StartDate, p.EndDate })
            .IsUnique();

        modelBuilder.Entity<ClientPeriodHours>().HasQueryFilter(p => !p.Client!.IsDeleted);

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

        modelBuilder.Entity<Client>()
         .HasMany(c => c.ClientContracts)
         .WithOne(cc => cc.Client)
         .HasForeignKey(cc => cc.ClientId)
         .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClientContract>()
         .HasOne(cc => cc.Contract)
         .WithMany()
         .HasForeignKey(cc => cc.ContractId)
         .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Client>()
         .HasOne(c => c.ClientImage)
         .WithOne(ci => ci.Client)
         .HasForeignKey<ClientImage>(ci => ci.ClientId)
         .IsRequired(false)
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

        modelBuilder.Entity<ContainerTemplate>()
        .HasOne(ct => ct.Shift)
        .WithMany()
        .HasForeignKey(ct => ct.ContainerId)
        .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ContainerTemplateItem>()
        .HasOne(cti => cti.ContainerTemplate)
        .WithMany(ct => ct.ContainerTemplateItems)
        .HasForeignKey(cti => cti.ContainerTemplateId)
        .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupItem>()
         .HasOne(gi => gi.Shift)
         .WithMany(s => s.GroupItems)
         .HasForeignKey(gi => gi.ShiftId)
         .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Work>()
               .HasOne(p => p.Client)
               .WithMany(b => b.Works)
               .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Work>()
            .HasOne(p => p.Shift);

        modelBuilder.Entity<Break>()
               .HasOne(p => p.Client)
               .WithMany(b => b.Breaks)
               .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AbsenceDetail>()
            .HasOne(p => p.Absence)
            .WithMany()
            .HasForeignKey(p => p.AbsenceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkChange>()
            .HasOne(sc => sc.Work)
            .WithMany()
            .HasForeignKey(sc => sc.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkChange>()
            .HasOne(sc => sc.ReplaceClient)
            .WithMany()
            .HasForeignKey(sc => sc.ReplaceClientId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Expenses>()
            .HasOne(e => e.Work)
            .WithMany()
            .HasForeignKey(e => e.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShiftExpenses>()
            .HasOne(e => e.Shift)
            .WithMany()
            .HasForeignKey(e => e.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);

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


        modelBuilder.Entity<Contract>()
           .HasOne(c => c.CalendarSelection)
           .WithMany()
           .HasForeignKey(c => c.CalendarSelectionId)
           .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SchedulingRule>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.IsDeleted, p.Name });
        });

        modelBuilder.Entity<Contract>()
           .HasOne(c => c.SchedulingRule)
           .WithMany()
           .HasForeignKey(c => c.SchedulingRuleId)
           .OnDelete(DeleteBehavior.SetNull);

        ConfigureAgentArchitecture(modelBuilder);
    }

    private static void ConfigureAgentArchitecture(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.IsDeleted, p.IsActive });
            entity.HasIndex(p => p.IsDefault).HasFilter("is_default = true AND is_deleted = false").IsUnique();
        });

        modelBuilder.Entity<AgentTemplate>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        modelBuilder.Entity<AgentLink>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.SourceAgentId, p.TargetAgentId, p.LinkType })
                .HasFilter("is_active = true AND is_deleted = false")
                .IsUnique();

            entity.HasOne(l => l.SourceAgent)
                .WithMany()
                .HasForeignKey(l => l.SourceAgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.TargetAgent)
                .WithMany()
                .HasForeignKey(l => l.TargetAgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgentSoulSection>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.AgentId, p.SectionType })
                .HasFilter("is_active = true AND is_deleted = false")
                .IsUnique();
            entity.HasIndex(p => new { p.AgentId, p.SortOrder })
                .HasFilter("is_active = true AND is_deleted = false");

            entity.HasOne(s => s.Agent)
                .WithMany(a => a.SoulSections)
                .HasForeignKey(s => s.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentSoulHistory>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.SoulSectionId, p.CreateTime });
            entity.HasIndex(p => new { p.AgentId, p.CreateTime });

            entity.HasOne(h => h.SoulSection)
                .WithMany(s => s.History)
                .HasForeignKey(h => h.SoulSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentMemory>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.AgentId, p.Category });
            entity.HasIndex(p => new { p.AgentId, p.Importance });
            entity.HasIndex(p => p.AgentId).HasFilter("is_pinned = true AND is_deleted = false");
            entity.HasIndex(p => p.ExpiresAt).HasFilter("expires_at IS NOT NULL AND is_deleted = false");
            entity.HasIndex(p => new { p.AgentId, p.Source });

            entity.HasOne(m => m.Agent)
                .WithMany(a => a.Memories)
                .HasForeignKey(m => m.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Supersedes)
                .WithMany()
                .HasForeignKey(m => m.SupersedesId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AgentMemoryTag>(entity =>
        {
            entity.HasKey(t => new { t.MemoryId, t.Tag });
            entity.HasIndex(t => t.Tag);

            entity.HasOne(t => t.Memory)
                .WithMany(m => m.Tags)
                .HasForeignKey(t => t.MemoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentSession>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.AgentId, p.Status });
            entity.HasIndex(p => new { p.UserId, p.UpdateTime });
            entity.HasIndex(p => new { p.AgentId, p.SessionId }).IsUnique();

            entity.HasOne(s => s.Agent)
                .WithMany(a => a.Sessions)
                .HasForeignKey(s => s.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentSessionMessage>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.SessionId, p.CreateTime });
            entity.HasIndex(p => new { p.SessionId, p.CreateTime })
                .HasFilter("is_compacted = false AND is_deleted = false");

            entity.HasOne(m => m.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.CompactedInto)
                .WithMany()
                .HasForeignKey(m => m.CompactedIntoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AgentSkill>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.AgentId, p.Name })
                .HasFilter("is_deleted = false")
                .IsUnique();
            entity.HasIndex(p => new { p.AgentId, p.IsEnabled, p.SortOrder });

            entity.HasOne(s => s.Agent)
                .WithMany(a => a.Skills)
                .HasForeignKey(s => s.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentSkillExecution>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);
            entity.HasIndex(p => new { p.SessionId, p.CreateTime });
            entity.HasIndex(p => new { p.SkillId, p.CreateTime });

            entity.HasOne(e => e.Skill)
                .WithMany(s => s.Executions)
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });
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
            const string defaultUser = "Anonymous";
            string currentUserName = defaultUser;

            try
            {
                var claimValue = httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(claimValue))
                {
                    currentUserName = claimValue;
                }
            }
            catch (ObjectDisposedException)
            {
            }

            switch (entry.State)
            {
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.CurrentValues[nameof(BaseEntity.IsDeleted)] = true;
                    entry.CurrentValues[nameof(BaseEntity.DeletedTime)] = now;
                    entry.CurrentValues[nameof(BaseEntity.CurrentUserDeleted)] = currentUserName!;

                    foreach (var property in entry.Properties)
                    {
                        property.IsModified = property.Metadata.Name == nameof(BaseEntity.IsDeleted) ||
                                             property.Metadata.Name == nameof(BaseEntity.DeletedTime) ||
                                             property.Metadata.Name == nameof(BaseEntity.CurrentUserDeleted);
                    }

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
