namespace support.server.Models;

public class NhanVien
{
    public int Id { get; set; }
    public string? MaNv { get; set; }
    public string? MatKhau { get; set; }
    public string? HoTen { get; set; }
    public string? HoTenKhongDau { get; set; }
    public DateOnly? NgaySinh { get; set; }
    public string? DiaChi { get; set; }
    public string? DienThoai { get; set; }
    public DateOnly? NgayVaoLam { get; set; }
    public int? IdPhongBan { get; set; }
    public int? IdTinhTrangLv { get; set; }
    public int? IdViTri { get; set; }
    public bool? IsGv { get; set; }
    public int? IdQuyen { get; set; }
    public string? Chukyhnhay { get; set; }
    public string? Chukychinhay { get; set; }
    public int? IdQuyenHt { get; set; }
    public string? GroupQuyen { get; set; }
    public string? Email { get; set; }
    public string? Cccd { get; set; }

    public virtual PhongBan? PhongBan { get; set; }
    public virtual Vitri? Vitri { get; set; }
}
