using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuanLyThietBiController : ControllerBase
    {
        private readonly EPortalDbContext _context;

        public QuanLyThietBiController(EPortalDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Danh sách quản lý thiết bị có filter phòng ban, loại thiết bị, lỗi sự cố, trạng thái, nhân viên
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> GetPagedList(
            int? idPhongBan = null,
            int? idTb = null,
            int? idSc = null,
            string? status = null,
            string? maNv = null,
            string? serviceTag = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            int page = 1,
            int pageSize = 20)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _context.NvQuanLyThietBis
                .Include(q => q.PhongBan)
                .Include(q => q.ThietBi)
                .Include(q => q.LoiSuCo)
                .AsQueryable();

            if (idPhongBan.HasValue)
                query = query.Where(q => q.IdPhongBan == idPhongBan.Value);

            if (idTb.HasValue)
                query = query.Where(q => q.IdTb == idTb.Value);

            if (idSc.HasValue)
                query = query.Where(q => q.IdSc == idSc.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(q => q.Status == status);

            if (!string.IsNullOrEmpty(maNv))
                query = query.Where(q => q.MaNv != null && q.MaNv.Contains(maNv));

            if (!string.IsNullOrEmpty(serviceTag))
                query = query.Where(q => q.ServiceTag != null && q.ServiceTag.Contains(serviceTag));

            if (fromDate.HasValue)
                query = query.Where(q => q.NgayLap >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(q => q.NgayLap <= toDate.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(q => q.NgayLap)
                .ThenByDescending(q => q.IdQltb)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new
                {
                    q.IdQltb,
                    q.IdPhongBan,
                    TenPhongBan = q.PhongBan != null ? q.PhongBan.TenPhongBan : null,
                    q.ServiceTag,
                    q.IdTb,
                    TenThietBi = q.ThietBi != null ? q.ThietBi.TenThietBi : null,
                    q.MaNv,
                    HoTenNv = _context.NhanViens
                        .Where(n => n.MaNv == q.MaNv)
                        .Select(n => n.HoTen)
                        .FirstOrDefault(),
                    q.Phone,
                    q.IdSc,
                    TenLoiSc = q.LoiSuCo != null ? q.LoiSuCo.TenLoiSc : null,
                    q.NgayLap,
                    q.Status,
                    q.NgayXl,
                    q.NgayHt,
                    q.NgayNm,
                    q.GhiChu,
                    q.AdminNm,
                    HoTenAdmin = _context.NhanViens
                        .Where(n => n.MaNv == q.AdminNm)
                        .Select(n => n.HoTen)
                        .FirstOrDefault()
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

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var item = await _context.NvQuanLyThietBis
                .Include(q => q.PhongBan)
                .Include(q => q.ThietBi)
                .Include(q => q.LoiSuCo)
                .Where(q => q.IdQltb == id)
                .Select(q => new
                {
                    q.IdQltb,
                    q.IdPhongBan,
                    TenPhongBan = q.PhongBan != null ? q.PhongBan.TenPhongBan : null,
                    q.ServiceTag,
                    q.IdTb,
                    TenThietBi = q.ThietBi != null ? q.ThietBi.TenThietBi : null,
                    q.MaNv,
                    HoTenNv = _context.NhanViens
                        .Where(n => n.MaNv == q.MaNv)
                        .Select(n => n.HoTen)
                        .FirstOrDefault(),
                    q.Phone,
                    q.IdSc,
                    TenLoiSc = q.LoiSuCo != null ? q.LoiSuCo.TenLoiSc : null,
                    q.NgayLap,
                    q.Status,
                    q.NgayXl,
                    q.NgayHt,
                    q.NgayNm,
                    q.GhiChu,
                    q.AdminNm,
                    HoTenAdmin = _context.NhanViens
                        .Where(n => n.MaNv == q.AdminNm)
                        .Select(n => n.HoTen)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<NvQuanLyThietBi>> Create([FromBody] NvQuanLyThietBi model)
        {
            model.NgayLap ??= DateOnly.FromDateTime(DateTime.Today);
            _context.NvQuanLyThietBis.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = model.IdQltb }, new { model.IdQltb });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] NvQuanLyThietBi model)
        {
            var item = await _context.NvQuanLyThietBis.FindAsync(id);
            if (item == null) return NotFound();

            item.IdPhongBan = model.IdPhongBan;
            item.ServiceTag = model.ServiceTag;
            item.IdTb = model.IdTb;
            item.MaNv = model.MaNv;
            item.Phone = model.Phone;
            item.IdSc = model.IdSc;
            item.NgayLap = model.NgayLap;
            item.Status = model.Status;
            item.NgayXl = model.NgayXl;
            item.NgayHt = model.NgayHt;
            item.NgayNm = model.NgayNm;
            item.GhiChu = model.GhiChu;
            item.AdminNm = model.AdminNm;

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var item = await _context.NvQuanLyThietBis.FindAsync(id);
            if (item == null) return NotFound();

            item.Status = request.Status;

            if (request.Status == "1")
                item.NgayXl = request.NgayXl ?? DateOnly.FromDateTime(DateTime.Today);
            else if (request.Status == "2")
                item.NgayHt = request.NgayHt ?? DateOnly.FromDateTime(DateTime.Today);
            else if (request.Status == "3")
                item.NgayNm = request.NgayNm ?? DateOnly.FromDateTime(DateTime.Today);

            if (!string.IsNullOrEmpty(request.GhiChu))
                item.GhiChu = request.GhiChu;

            if (!string.IsNullOrEmpty(request.AdminNm))
                item.AdminNm = request.AdminNm;

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.NvQuanLyThietBis.FindAsync(id);
            if (item == null) return NotFound();

            _context.NvQuanLyThietBis.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class UpdateStatusRequest
    {
        public string? Status { get; set; }
        public DateOnly? NgayXl { get; set; }
        public DateOnly? NgayHt { get; set; }
        public DateOnly? NgayNm { get; set; }
        public string? GhiChu { get; set; }
        public string? AdminNm { get; set; }
    }
}
