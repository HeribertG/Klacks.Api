// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Models.Histories;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Domain.Models.FloorPlans;
using Klacks.Api.Domain.Models.Klacksy;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.KnowledgeIndex.Domain;
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

    public DbSet<ClientShiftPreference> ClientShiftPreference { get; set; }

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

    public DbSet<ContainerShiftOverride> ContainerShiftOverrides { get; set; }

    public DbSet<ContainerShiftOverrideItem> ContainerShiftOverrideItems { get; set; }

    public DbSet<WizardTrainingRun> WizardTrainingRuns { get; set; }

    public DbSet<ContainerLock> ContainerLock { get; set; }

    public DbSet<State> State { get; set; }

    public DbSet<Work> Work { get; set; }

    public DbSet<PeriodAuditLog> PeriodAuditLog { get; set; }

    public DbSet<ExportLog> ExportLog { get; set; }

    public DbSet<Break> Break { get; set; }

    public DbSet<WorkChange> WorkChange { get; set; }

    public DbSet<Expenses> Expenses { get; set; }

    public DbSet<ScheduleNote> ScheduleNotes { get; set; }

    public DbSet<ScheduleCommand> ScheduleCommands { get; set; }

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

    public DbSet<LLMSyncNotification> LLMSyncNotifications { get; set; }

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

    // Knowledge Index DbSets
    public DbSet<KnowledgeEntry> KnowledgeEntries { get; set; }

    // Transcription Dictionary DbSets
    public DbSet<TranscriptionDictionaryEntry> TranscriptionDictionaryEntries { get; set; }

    // Custom STT Provider DbSets
    public DbSet<CustomSttProvider> CustomSttProviders { get; set; }

    // FloorPlan DbSets
    public DbSet<FloorPlan> FloorPlan { get; set; }

    public DbSet<FloorPlanWorkMarker> FloorPlanWorkMarker { get; set; }

    // Klacksy DbSets
    public DbSet<KlacksyNavigationFeedback> KlacksyNavigationFeedback => Set<KlacksyNavigationFeedback>();

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

        modelBuilder.Entity<ContainerLock>(entity =>
        {
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId }).IsUnique();
        });

        modelBuilder.Entity<KnowledgeEntry>(entity =>
        {
            entity.ToTable(KnowledgeIndex.Application.Constants.KnowledgeIndexConstants.TableName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Kind).HasColumnName("kind");
            entity.Property(e => e.SourceId).HasColumnName("source_id");
            entity.Property(e => e.Text).HasColumnName("text");
            entity.Property(e => e.TextHash).HasColumnName("text_hash");
            entity.Property(e => e.RequiredPermission).HasColumnName("required_permission");
            entity.Property(e => e.ExposedEndpointKey).HasColumnName("exposed_endpoint_key");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => new { e.Kind, e.SourceId }).IsUnique().HasDatabaseName("knowledge_index_kind_source_unique");
        });

        modelBuilder.Entity<TranscriptionDictionaryEntry>(entity =>
        {
            entity.ToTable("transcription_dictionary_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrectTerm).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PhoneticVariants).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<CustomSttProvider>(entity =>
        {
            entity.ToTable("custom_stt_providers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ConnectionType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ApiUrl).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ApiKey).HasMaxLength(500);
            entity.Property(e => e.LanguageModel).HasMaxLength(200);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<LLMSyncNotification>(entity =>
        {
            entity.ToTable("llm_sync_notifications");
            entity.Property(e => e.NewModelNames)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            entity.Property(e => e.DeactivatedModelNames)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            entity.Property(e => e.ModelTestResults)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<LLMModelTestResult>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<LLMModelTestResult>());
        });

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
