using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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
            entity.Property(e => e.FileAttachments)
                .HasMaxLength(500)
                .HasColumnName("file_attachments");
            entity.Property(e => e.Note)
                .HasMaxLength(150)
                .HasColumnName("note");
            entity.Property(e => e.TicketCode)
                .HasMaxLength(20)
                .HasColumnName("ticket_code");
            entity.Property(e => e.TicketContent)
                .HasMaxLength(1000)
                .HasColumnName("ticket_content");
            entity.Property(e => e.TicketStatus).HasColumnName("ticket_status");
            entity.Property(e => e.TicketTitle)
                .HasMaxLength(500)
                .HasColumnName("ticket_title");
            entity.Property(e => e.TicketType)
                .HasMaxLength(20)
                .HasColumnName("ticket_type");
            entity.Property(e => e.UserAssigneeCode)
                .HasMaxLength(20)
                .HasColumnName("user_assignee_code");
            entity.Property(e => e.UserAssigneeDepartment)
                .HasMaxLength(100)
                .IsFixedLength()
                .HasColumnName("user_assignee_department");
            entity.Property(e => e.UserAssigneeName)
                .HasMaxLength(50)
                .HasColumnName("user_assignee_name");
            entity.Property(e => e.UserCode)
                .HasMaxLength(12)
                .HasColumnName("user_code");
            entity.Property(e => e.UserContact)
                .HasMaxLength(50)
                .HasColumnName("user_contact");
            entity.Property(e => e.UserDepartment)
                .HasMaxLength(100)
                .HasColumnName("user_department");
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .HasColumnName("user_name");
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PK__permissi__E5331AFA3010F562");

            entity.ToTable("user_permissions");

            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PermissionCode)
                .HasMaxLength(20)
                .HasColumnName("permission_code");
            entity.Property(e => e.UserCode)
                .HasMaxLength(12)
                .HasColumnName("user_code");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
