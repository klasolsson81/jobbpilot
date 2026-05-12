using JobbPilot.Domain.Invitations;
using JobbPilot.Domain.Waitlist;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobbPilot.Infrastructure.Persistence.Configurations;

public sealed class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> builder)
    {
        builder.ToTable("waitlist_entries");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .HasConversion(id => id.Value, value => new WaitlistEntryId(value))
            .ValueGeneratedNever();

        builder.Property(w => w.Email)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(w => w.RequestedAt).IsRequired();

        builder.Property(w => w.Status)
            .HasConversion(
                s => s.Name,
                v => WaitlistStatus.FromName(v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.ApprovedAt);
        builder.Property(w => w.ApprovedByAdminId);
        builder.Property(w => w.RejectedAt);
        builder.Property(w => w.RejectedByAdminId);

        builder.Property(w => w.ResultingInvitationId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (InvitationId?)null : new InvitationId(value.Value));

        // Admin pollar "vilka pending finns" → status-first compound index.
        builder.HasIndex(w => new { w.Status, w.RequestedAt });

        // Spam-skydd och dedup: en email kan inte ha flera Pending-poster.
        // Approved/Rejected får dock samexistera med ny Pending (re-application
        // efter rejection). Partial index löser detta — filter på Status.
        builder.HasIndex(w => w.Email)
            .HasFilter("status = 'Pending'")
            .IsUnique();

        builder.Ignore(w => w.DomainEvents);
    }
}
