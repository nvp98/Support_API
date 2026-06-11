using Microsoft.EntityFrameworkCore;

namespace support.server.Models;

public partial class EPortalDbContext : DbContext
{
    public EPortalDbContext() { }

    public EPortalDbContext(DbContextOptions<EPortalDbContext> options) : base(options) { }

    public virtual DbSet<NhanVien> NhanViens { get; set; }
    public virtual DbSet<PhongBan> PhongBans { get; set; }
    public virtual DbSet<Vitri> Vitris { get; set; }
    public virtual DbSet<NvThietBi> NvThietBis { get; set; }
    public virtual DbSet<NvLoiSuCoTb> NvLoiSuCoTbs { get; set; }
    public virtual DbSet<NvQuanLyThietBi> NvQuanLyThietBis { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_NhanVien");
            entity.ToTable("NhanVien");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.MaNv).HasMaxLength(11).HasColumnName("MaNV");
            entity.Property(e => e.MatKhau).HasMaxLength(50).HasColumnName("MatKhau");
            entity.Property(e => e.HoTen).HasMaxLength(50).HasColumnName("HoTen");
            entity.Property(e => e.HoTenKhongDau).HasMaxLength(50).HasColumnName("HoTenKhongDau");
            entity.Property(e => e.NgaySinh).HasColumnName("NgaySinh").HasColumnType("date");
            entity.Property(e => e.DiaChi).HasMaxLength(150).HasColumnName("DiaChi");
            entity.Property(e => e.DienThoai).HasMaxLength(11).HasColumnName("DienThoai");
            entity.Property(e => e.NgayVaoLam).HasColumnName("NgayVaoLam").HasColumnType("date");
            entity.Property(e => e.IdPhongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.IdTinhTrangLv).HasColumnName("IDTinhTrangLV");
            entity.Property(e => e.IdViTri).HasColumnName("IDViTri");
            entity.Property(e => e.IsGv).HasColumnName("IsGV");
            entity.Property(e => e.IdQuyen).HasColumnName("IDQuyen");
            entity.Property(e => e.Chukyhnhay).HasMaxLength(100).HasColumnName("Chukyhnhay");
            entity.Property(e => e.Chukychinhay).HasMaxLength(100).HasColumnName("Chukychinhay");
            entity.Property(e => e.IdQuyenHt).HasColumnName("IDQuyenHT");
            entity.Property(e => e.GroupQuyen).HasMaxLength(50).HasColumnName("GroupQuyen");
            entity.Property(e => e.Email).HasMaxLength(50).HasColumnName("Email");
            entity.Property(e => e.Cccd).HasMaxLength(15).HasColumnName("CCCD");

            entity.HasOne(e => e.PhongBan)
                .WithMany(p => p.NhanViens)
                .HasForeignKey(e => e.IdPhongBan)
                .HasConstraintName("FK_NhanVien_PhongBan");

            entity.HasOne(e => e.Vitri)
                .WithMany(v => v.NhanViens)
                .HasForeignKey(e => e.IdViTri)
                .HasConstraintName("FK_NhanVien_Vitri");
        });

        modelBuilder.Entity<PhongBan>(entity =>
        {
            entity.HasKey(e => e.IdPhongBan).HasName("PK_PhongBan");
            entity.ToTable("PhongBan");

            entity.Property(e => e.IdPhongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.TenPhongBan).HasMaxLength(100).HasColumnName("TenPhongBan");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        modelBuilder.Entity<Vitri>(entity =>
        {
            entity.HasKey(e => e.IdViTri).HasName("PK_Vitri");
            entity.ToTable("Vitri");

            entity.Property(e => e.IdViTri).HasColumnName("IDViTri");
            entity.Property(e => e.TenViTri).HasMaxLength(50).HasColumnName("TenViTri");
        });

        modelBuilder.Entity<NvThietBi>(entity =>
        {
            entity.HasKey(e => e.IdTb).HasName("PK_NV_ThietBi");
            entity.ToTable("NV_ThietBi");

            entity.Property(e => e.IdTb).HasColumnName("IDTB");
            entity.Property(e => e.TenThietBi).HasMaxLength(100).HasColumnName("TenThietBi");
        });

        modelBuilder.Entity<NvLoiSuCoTb>(entity =>
        {
            entity.HasKey(e => e.IdSc).HasName("PK_NV_LoiSuCoTB");
            entity.ToTable("NV_LoiSuCoTB");

            entity.Property(e => e.IdSc).HasColumnName("IDSC");
            entity.Property(e => e.TenLoiSc).HasMaxLength(100).HasColumnName("TenLoiSC");
        });

        modelBuilder.Entity<NvQuanLyThietBi>(entity =>
        {
            entity.HasKey(e => e.IdQltb).HasName("PK_NV_QuanLyThietBi");
            entity.ToTable("NV_QuanLyThietBi");

            entity.Property(e => e.IdQltb).HasColumnName("IDQLTB");
            entity.Property(e => e.IdPhongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.ServiceTag).HasMaxLength(30).HasColumnName("ServiceTag");
            entity.Property(e => e.IdTb).HasColumnName("IDTB");
            entity.Property(e => e.MaNv).HasMaxLength(10).HasColumnName("MaNV");
            entity.Property(e => e.Phone).HasMaxLength(10).HasColumnName("Phone");
            entity.Property(e => e.IdSc).HasColumnName("IDSC");
            entity.Property(e => e.NgayLap).HasColumnName("NgayLap").HasColumnType("date");
            entity.Property(e => e.Status).HasMaxLength(1).IsFixedLength().HasColumnName("Status");
            entity.Property(e => e.NgayXl).HasColumnName("NgayXL").HasColumnType("date");
            entity.Property(e => e.NgayHt).HasColumnName("NgayHT").HasColumnType("date");
            entity.Property(e => e.NgayNm).HasColumnName("NgayNM").HasColumnType("date");
            entity.Property(e => e.GhiChu).HasMaxLength(500).HasColumnName("GhiChu");
            entity.Property(e => e.AdminNm).HasMaxLength(10).HasColumnName("AdminNM");

            entity.HasOne(e => e.PhongBan)
                .WithMany()
                .HasForeignKey(e => e.IdPhongBan)
                .HasConstraintName("FK_NV_QuanLyThietBi_PhongBan");

            entity.HasOne(e => e.ThietBi)
                .WithMany()
                .HasForeignKey(e => e.IdTb)
                .HasConstraintName("FK_NV_QuanLyThietBi_NV_ThietBi");

            entity.HasOne(e => e.LoiSuCo)
                .WithMany()
                .HasForeignKey(e => e.IdSc)
                .HasConstraintName("FK_NV_QuanLyThietBi_NV_LoiSuCoTB");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
