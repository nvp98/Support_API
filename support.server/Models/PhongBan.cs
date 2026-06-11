namespace support.server.Models;

public class PhongBan
{
    public int IdPhongBan { get; set; }
    public string? TenPhongBan { get; set; }
    public int? Status { get; set; }

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
