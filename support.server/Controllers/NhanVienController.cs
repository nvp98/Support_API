using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NhanVienController : ControllerBase
    {
        private readonly EPortalDbContext _context;

        public NhanVienController(EPortalDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Danh sách nhân viên có filter phòng ban, vị trí, tên, mã NV
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> GetList(
            int? idPhongBan = null,
            int? idViTri = null,
            int? idTinhTrangLv = null,
            string? keyword = null,
            bool? isGv = null,
            int page = 1,
            int pageSize = 50)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 50;

            var query = _context.NhanViens
                .Include(n => n.PhongBan)
                .Include(n => n.Vitri)
                .AsQueryable();

            if (idPhongBan.HasValue)
                query = query.Where(n => n.IdPhongBan == idPhongBan.Value);

            if (idViTri.HasValue)
                query = query.Where(n => n.IdViTri == idViTri.Value);

            if (idTinhTrangLv.HasValue)
                query = query.Where(n => n.IdTinhTrangLv == idTinhTrangLv.Value);

            if (isGv.HasValue)
                query = query.Where(n => n.IsGv == isGv.Value);

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(n =>
                    (n.HoTen != null && n.HoTen.Contains(keyword)) ||
                    (n.HoTenKhongDau != null && n.HoTenKhongDau.Contains(keyword)) ||
                    (n.MaNv != null && n.MaNv.Contains(keyword)) ||
                    (n.Email != null && n.Email.Contains(keyword)));

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(n => n.HoTen)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    n.Id,
                    n.MaNv,
                    n.HoTen,
                    n.HoTenKhongDau,
                    n.NgaySinh,
                    n.DiaChi,
                    n.DienThoai,
                    n.NgayVaoLam,
                    n.IdPhongBan,
                    TenPhongBan = n.PhongBan != null ? n.PhongBan.TenPhongBan : null,
                    n.IdViTri,
                    TenViTri = n.Vitri != null ? n.Vitri.TenViTri : null,
                    n.IsGv,
                    n.Email,
                    n.IdQuyen,
                    n.GroupQuyen
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                items
            });
        }

        [HttpGet("all")]
        public async Task<ActionResult<object>> GetAll(int? idPhongBan = null, bool? isGv = null)
        {
            var query = _context.NhanViens
                .Include(n => n.PhongBan)
                .Include(n => n.Vitri)
                .AsQueryable();

            if (idPhongBan.HasValue)
                query = query.Where(n => n.IdPhongBan == idPhongBan.Value);

            if (isGv.HasValue)
                query = query.Where(n => n.IsGv == isGv.Value);

            var items = await query
                .OrderBy(n => n.HoTen)
                .Select(n => new
                {
                    n.Id,
                    n.MaNv,
                    n.HoTen,
                    n.DienThoai,
                    n.Email,
                    n.IdPhongBan,
                    TenPhongBan = n.PhongBan != null ? n.PhongBan.TenPhongBan : null,
                    n.IdViTri,
                    TenViTri = n.Vitri != null ? n.Vitri.TenViTri : null
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var nv = await _context.NhanViens
                .Include(n => n.PhongBan)
                .Include(n => n.Vitri)
                .Where(n => n.Id == id)
                .Select(n => new
                {
                    n.Id,
                    n.MaNv,
                    n.HoTen,
                    n.HoTenKhongDau,
                    n.NgaySinh,
                    n.DiaChi,
                    n.DienThoai,
                    n.NgayVaoLam,
                    n.IdPhongBan,
                    TenPhongBan = n.PhongBan != null ? n.PhongBan.TenPhongBan : null,
                    n.IdViTri,
                    TenViTri = n.Vitri != null ? n.Vitri.TenViTri : null,
                    n.IsGv,
                    n.Email,
                    n.Cccd,
                    n.IdQuyen,
                    n.IdQuyenHt,
                    n.GroupQuyen
                })
                .FirstOrDefaultAsync();

            if (nv == null) return NotFound();
            return Ok(nv);
        }
    }
}
