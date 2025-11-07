using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketLogsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TicketLogsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetPagedList(
        int page = 1,
        int pageSize = 10,
        byte? status = null,
        string department = null,
        string type = null,
        string keyword = null,
        string usercode = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.TicketLogs.AsQueryable();

            // Filter theo status
            if (status.HasValue)
                query = query.Where(t => t.TicketStatus == status.Value);

            // Filter theo department
            if (!string.IsNullOrEmpty(department))
                query = query.Where(t => t.UserDepartment.Contains(department));

            // Filter theo type
            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.TicketType.Contains(type));

            // Filter theo usercode
            if (!string.IsNullOrEmpty(usercode))
                query = query.Where(t => t.UserCode.Contains(usercode));

            // Filter theo keyword (ticketCode, userName)
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(t =>
                    t.TicketCode.Contains(keyword) ||
                    t.UserName.Contains(keyword));

            // Filter theo CreatedAt: từ ngày
            if (fromDate.HasValue)
                query = query.Where(t => t.CreatedAt >= fromDate.Value);

            // Filter theo CreatedAt: đến ngày
            if (toDate.HasValue)
                query = query.Where(t => t.CreatedAt <= toDate.Value);

            // Tổng số bản ghi
            var totalRecords = await query.CountAsync();

            // Phân trang
            var items = await query
                .OrderBy(t => t.TicketStatus)                 // sắp xếp theo trạng thái trước
                .ThenByDescending(t => t.CreatedAt)     // sau đó mới sắp xếp theo ngày tạo mới nhất
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Build link file cho response
            var baseUrl = $"{Request.Scheme}://{Request.Host}/uploads";
            var result = items.Select(t => new
            {
                t.TicketId,
                t.TicketCode,
                t.TicketTitle,
                t.TicketType,
                t.TicketContent,
                t.TicketStatus,
                FileUrl = string.IsNullOrEmpty(t.FileAttachments) ? null : $"{baseUrl}/{t.FileAttachments}",
                t.CreatedAt,
                t.UserCode,
                t.UserName,
                t.UserDepartment,
                t.UserContact,
                t.UserAssigneeCode,
                t.UserAssigneeName,
                t.UserAssigneeDepartment,
                t.ApprovedAt,
                t.ReceivedAt,
                t.Note
            });

            return Ok(new
            {
                totalRecords,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                items = result
            });
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<TicketLog>> GetById(int id)
        {
            var ticket = await _context.TicketLogs.FindAsync(id);
            if (ticket == null) return NotFound();
            return ticket;
        }

        [HttpPost("create")]
        [RequestSizeLimit(20_000_000)]
        public async Task<ActionResult<TicketLog>> Create(TicketLog ticket)
        {
            // 🔹 Sinh mã ticket tự động
            var today = DateTime.Now.ToString("yyMMdd");
            var countToday = await _context.TicketLogs.CountAsync(t => t.CreatedAt.Value.Date == DateTime.Today);
            var newCode = $"{ticket.TicketType}-{today}-{(countToday + 1).ToString("D4")}";

            string savedFileName = null;
            if (ticket.UploadedFile != null)
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                savedFileName = $"{Guid.NewGuid()}_{ticket.UploadedFile.FileName}";
                var filePath = Path.Combine(uploadPath, savedFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ticket.UploadedFile.CopyToAsync(stream);
                }

                ticket.FileAttachments = savedFileName; // lưu tên file vào DB
            }
            ticket.TicketCode = newCode; // override client gửi lên
            ticket.TicketStatus = 0;
            ticket.CreatedAt = DateTime.Now;
            _context.TicketLogs.Add(ticket);
            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<TicketLog>
            {
                status = 201,
                message = "Tạo ticket thành công!",
                Data = ticket
            });
            //return CreatedAtAction(nameof(GetById), new { id = ticket.TicketId }, ticket);
        }

        [HttpPut("received/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TicketLog model)
        {
            var ticket = await _context.TicketLogs.FindAsync(id);
            if (ticket == null) return NotFound();

            // Cập nhật các trường cần thiết
            ticket.TicketStatus = 1; // tiếp nhận
            ticket.UserAssigneeCode = model.UserAssigneeCode;
            ticket.UserAssigneeName = model.UserAssigneeName;
            ticket.UserAssigneeDepartment = model.UserAssigneeDepartment;
            ticket.ReceivedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(ticket);
        }

        [HttpPut("reset/{id}")]
        public async Task<IActionResult> UpdateReset(int id, [FromBody] TicketLog model)
        {
            var ticket = await _context.TicketLogs.FindAsync(id);
            if (ticket == null) return NotFound();

            // Cập nhật các trường cần thiết
            ticket.TicketStatus = 0; // reset về chờ tiếp nhận
            ticket.UserAssigneeCode = null;
            ticket.UserAssigneeName = null;
            ticket.UserAssigneeDepartment = null;
            ticket.ReceivedAt = null;

            await _context.SaveChangesAsync();
            return Ok(ticket);
        }

        [HttpPut("completed/{id}")]
        public async Task<IActionResult> UpdateCompleted(int id, [FromBody] TicketLog model)
        {
            var ticket = await _context.TicketLogs.FindAsync(id);
            if (ticket == null)
                return NotFound("Không tìm thấy ticket.");
            //if(ticket.UserAssigneeCode != model.UserAssigneeCode)
            //    return NotFound("Ticket này chỉ được đóng với user đã tiếp nhận.");
            // Cập nhật trạng thái và thông tin hoàn tất
            ticket.TicketStatus = 2; // ví dụ: "Hoàn thành" hoặc mã 2
            ticket.Note = model.Note;                 // ghi chú kết quả xử lý
            ticket.ApprovedAt = DateTime.Now;         // thời điểm hoàn tất / phê duyệt

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật hoàn tất ticket thành công.",
                ticket
            });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.TicketLogs.FindAsync(id);
            if (ticket == null) return NotFound();
            _context.TicketLogs.Remove(ticket);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
