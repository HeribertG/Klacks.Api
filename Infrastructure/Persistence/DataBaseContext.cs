// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Models.Histories;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Domain.Models.FloorPlans;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
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

    public DbSet<ClientAvailabilityScheduleEntry> ClientAvailabilityScheduleEntries { get; set; }

    public DbSet<ContainerTemplate> ContainerTemplate { get; set; }

    public DbSet<ContainerTemplateItem> ContainerTemplateItem { get; set; }

    public DbSet<State> State { get; set; }

    public DbSet<Work> Work { get; set; }

    public DbSet<Break> Break { get; set; }

    public DbSet<WorkChange> WorkChange { get; set; }

    public DbSet<Expenses> Expenses { get; set; }

    public DbSet<ScheduleNote> ScheduleNotes { get; set; }

    public DbSet<AnalyseScenario> AnalyseScenarios { get; set; }

    public DbSet<ShiftExpenses> ShiftExpenses { get; set; }

    public DbSet<ScheduleChange> ScheduleChange { get; set; }

    public DbSet<ClientPeriodHours> ClientPeriodHours { get; set; }

    public DbSet<IndividualPeriod> IndividualPeriod { get; set; }

    public DbSet<Period> Period { get; set; }

    public DbSet<AssignedGroup> AssignedGroup { get; set; }  

    public DbSet<GroupVisibility> GroupVisibility { get; set; }

    public DbSet<Contract> Contract { get; set; }

    public DbSet<ClientContract> ClientContract { get; set; }

    public DbSet<ClientAvailability> ClientAvailability { get; set; }

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

    // Global Agent Rules DbSets
    public DbSet<GlobalAgentRule> GlobalAgentRules { get; set; }
    public DbSet<GlobalAgentRuleHistory> GlobalAgentRuleHistories { get; set; }

    // UI Control Registry DbSet
    public DbSet<UiControl> UiControls { get; set; }

    // Skill Synonym DbSet
    public DbSet<SkillSynonym> SkillSynonyms { get; set; }

    // Skill Gap Detection DbSet
    public DbSet<SkillGapRecord> SkillGapRecords { get; set; }

    // Scheduling DbSets
    public DbSet<SchedulingRule> SchedulingRules { get; set; }

    // Report DbSets
    public DbSet<ReportTemplate> ReportTemplates { get; set; }

    // Email DbSets
    public DbSet<ReceivedEmail> ReceivedEmails { get; set; }
    public DbSet<EmailFolder> EmailFolders { get; set; }
    public DbSet<SpamRule> SpamRules { get; set; }

    // Plugin DbSets
    public DbSet<PluginDoc> PluginDocs { get; set; }

    // Sentiment DbSets
    public DbSet<SentimentKeywordSet> SentimentKeywordSets { get; set; }

    // FloorPlan DbSets
    public DbSet<FloorPlan> FloorPlan { get; set; }

    public DbSet<FloorPlanWorkMarker> FloorPlanWorkMarker { get; set; }

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
        base.OnModelCreating(modelBuilder);
        modelBuilder.RegisterMultiLanguageDbFunctions();

        modelBuilder.HasSequence<int>("client_idnumber_seq", schema: "public")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        Plugins.PluginModelRegistry.Apply(modelBuilder);
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
