namespace support.server.Models;

public class Vitri
{
    public int IdViTri { get; set; }
    public string? TenViTri { get; set; }

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
