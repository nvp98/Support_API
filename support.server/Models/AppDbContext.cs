using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using support.server.Models.SLC;

namespace support.server.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Permision> Permisions { get; set; }
    public virtual DbSet<TicketLog> TicketLogs { get; set; }
    public virtual DbSet<UserPermission> UserPermissions { get; set; }

    // SLC Module
    public virtual DbSet<SoftwareCatalog> SoftwareCatalogs { get; set; }
    public virtual DbSet<SlcProject> SlcProjects { get; set; }
    public virtual DbSet<BusinessProcess> BusinessProcesses { get; set; }
    public virtual DbSet<ProcessStep> ProcessSteps { get; set; }
    public virtual DbSet<SlcModule> SlcModules { get; set; }
    public virtual DbSet<ModuleProcessStep> ModuleProcessSteps { get; set; }
    public virtual DbSet<SlcTask> SlcTasks { get; set; }
    public virtual DbSet<ChangeRequest> ChangeRequests { get; set; }
    public virtual DbSet<ChangeRevision> ChangeRevisions { get; set; }
    public virtual DbSet<TimelineAdjustment> TimelineAdjustments { get; set; }
    public virtual DbSet<ChangeRequestRole> ChangeRequestRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permision>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("permisions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PermisonCode)
                .HasMaxLength(20)
                .HasColumnName("permison_code");
        });

        modelBuilder.Entity<TicketLog>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__ticket_l__D596F96BCD398B9B");
            entity.ToTable("ticket_logs");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ReceivedAt).HasColumnName("received_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.FileAttachments).HasMaxLength(500).HasColumnName("file_attachments");
            entity.Property(e => e.Note).HasMaxLength(150).HasColumnName("note");
            entity.Property(e => e.CompletedNote).HasColumnName("completedNote");
            entity.Property(e => e.ProcessingMinutes).HasColumnName("processingMinutes");
            entity.Property(e => e.ErrorClassification).HasMaxLength(10).HasColumnName("errorClassification");
            entity.Property(e => e.HandlerClassification).HasMaxLength(10).HasColumnName("handlerClassification");
            entity.Property(e => e.TicketCode).HasMaxLength(20).HasColumnName("ticket_code");
            entity.Property(e => e.TicketContent).HasMaxLength(1000).HasColumnName("ticket_content");
            entity.Property(e => e.TicketStatus).HasColumnName("ticket_status");
            entity.Property(e => e.TicketTitle).HasMaxLength(500).HasColumnName("ticket_title");
            entity.Property(e => e.TicketType).HasMaxLength(20).HasColumnName("ticket_type");
            entity.Property(e => e.UserAssigneeCode).HasMaxLength(20).HasColumnName("user_assignee_code");
            entity.Property(e => e.UserAssigneeDepartment).HasMaxLength(100).IsFixedLength().HasColumnName("user_assignee_department");
            entity.Property(e => e.UserAssigneeName).HasMaxLength(50).HasColumnName("user_assignee_name");
            entity.Property(e => e.UserCode).HasMaxLength(12).HasColumnName("user_code");
            entity.Property(e => e.UserContact).HasMaxLength(50).HasColumnName("user_contact");
            entity.Property(e => e.UserDepartment).HasMaxLength(100).HasColumnName("user_department");
            entity.Property(e => e.UserName).HasMaxLength(50).HasColumnName("user_name");
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PK__permissi__E5331AFA3010F562");
            entity.ToTable("user_permissions");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.PermissionCode).HasMaxLength(20).HasColumnName("permission_code");
            entity.Property(e => e.UserCode).HasMaxLength(12).HasColumnName("user_code");
        });

        // SLC: BusinessProcess self-referencing hierarchy
        modelBuilder.Entity<BusinessProcess>(entity =>
        {
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SLC: ModuleProcessStep composite primary key
        modelBuilder.Entity<ModuleProcessStep>(entity =>
        {
            entity.HasKey(e => new { e.ModuleId, e.ProcessStepId });
        });

        // SLC: ChangeRequest optional FK to Module and Project
        modelBuilder.Entity<ChangeRequest>(entity =>
        {
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(e => e.Module)
                .WithMany(m => m.ChangeRequests)
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Project)
                .WithMany(p => p.ChangeRequests)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChangeRequestRole>(entity =>
        {
            entity.ToTable("slc_change_request_roles");
            entity.HasIndex(e => new { e.UserCode, e.RoleCode })
                .HasDatabaseName("UX_slc_change_request_roles_user_role")
                .IsUnique();
        });

        // SLC: TimelineAdjustment optional FK to ChangeRequest
        modelBuilder.Entity<TimelineAdjustment>(entity =>
        {
            entity.HasOne(e => e.ChangeRequest)
                .WithMany()
                .HasForeignKey(e => e.ChangeRequestId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
