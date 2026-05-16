using JobbPilot.Domain.JobSeekers;
using JobbPilot.Domain.SavedSearches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobbPilot.Infrastructure.Persistence.Configurations;

public sealed class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.ToTable("saved_searches");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new SavedSearchId(value))
            .ValueGeneratedNever();

        builder.Property(s => s.JobSeekerId)
            .HasConversion(id => id.Value, value => new JobSeekerId(value))
            .IsRequired();
        // Scope-query :as på (JobSeekerId) i alla handlers — B-tree-index för
        // "mina sparade sökningar" + JobSeeker-scopad lookup.
        builder.HasIndex(s => s.JobSeekerId)
            .HasDatabaseName("ix_saved_searches_job_seeker_id");

        builder.Property(s => s.Name)
            .HasMaxLength(SavedSearch.NameMaxLength)
            .IsRequired();

        // ADR 0039 §16 — criteria jsonb. Owned-type-to-json speglar
        // JobSeekerConfiguration.Preferences-mönstret (.ToJson()).
        builder.OwnsOne(s => s.Criteria, criteria =>
        {
            criteria.ToJson();
        });

        builder.Property(s => s.NotificationEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        // ADR 0039 Beslut 2 — kolumnen finns (schema-stabil), skrivlogik Fas 5.
        builder.Property(s => s.LastRunAt);

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.DeletedAt);

        builder.HasQueryFilter(s => s.DeletedAt == null);

        builder.Ignore(s => s.DomainEvents);
    }
}
