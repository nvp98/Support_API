namespace support.server.Models;

public class NvQuanLyThietBi
{
    public int IdQltb { get; set; }
    public int? IdPhongBan { get; set; }
    public string? ServiceTag { get; set; }
    public int? IdTb { get; set; }
    public string? MaNv { get; set; }
    public string? Phone { get; set; }
    public int? IdSc { get; set; }
    public DateOnly? NgayLap { get; set; }
    public string? Status { get; set; }
    public DateOnly? NgayXl { get; set; }
    public DateOnly? NgayHt { get; set; }
    public DateOnly? NgayNm { get; set; }
    public string? GhiChu { get; set; }
    public string? AdminNm { get; set; }

    public virtual PhongBan? PhongBan { get; set; }
    public virtual NvThietBi? ThietBi { get; set; }
    public virtual NvLoiSuCoTb? LoiSuCo { get; set; }
}
