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
        string userAssigneeCode = null,
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

            // Filter theo usercode
            if (!string.IsNullOrEmpty(userAssigneeCode))
                query = query.Where(t => t.UserAssigneeCode.Contains(userAssigneeCode));

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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] TicketLog model)
        {
            var ticket = await _context.TicketLogs.FindAsync(id);
            if (ticket == null)
                return NotFound("Không tìm thấy ticket.");
            // Cập nhật thông tin ticket
            ticket.TicketCode = model.TicketType + model.TicketCode?.Substring('-');
            ticket.TicketContent = model.TicketContent;
            ticket.TicketType = model.TicketType;
            ticket.TicketTitle = model.TicketTitle;
            ticket.UserContact = model.UserContact;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật hoàn tất ticket thành công.",
                ticket
            });
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

        [HttpPut("cancel/{id}")]
        public async Task<IActionResult> UpdateCancel(int id, [FromBody] TicketLog model)
        {
            var ticket = await _context.TicketLogs.FindAsync(id);
            if (ticket == null)
                return NotFound("Không tìm thấy ticket.");
            //if(ticket.UserAssigneeCode != model.UserAssigneeCode)
            //    return NotFound("Ticket này chỉ được đóng với user đã tiếp nhận.");
            // Cập nhật trạng thái và thông tin hoàn tất
            ticket.TicketStatus = 3; // Hủy ticket
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

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel(
    byte? status = null,
    string department = null,
    string type = null,
    string keyword = null,
    string usercode = null,
    string userAssigneeCode = null,
    DateTime? fromDate = null,
    DateTime? toDate = null)
        {
            // ==== 1. Lọc dữ liệu tương tự GetPagedList ====
            var query = _context.TicketLogs.AsQueryable();

            if (status.HasValue)
                query = query.Where(t => t.TicketStatus == status.Value);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(t => t.UserDepartment.Contains(department));

            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.TicketType.Contains(type));

            if (!string.IsNullOrEmpty(usercode))
                query = query.Where(t => t.UserCode.Contains(usercode));

            if (!string.IsNullOrEmpty(userAssigneeCode))
                query = query.Where(t => t.UserAssigneeCode.Contains(userAssigneeCode));

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(t => t.TicketCode.Contains(keyword) || t.UserName.Contains(keyword));

            if (fromDate.HasValue)
                query = query.Where(t => t.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.CreatedAt <= toDate.Value);

            var items = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            // ==== 2. Đường dẫn đến template ====
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TicketLog_Template.xlsx");
            if (!System.IO.File.Exists(templatePath))
                return NotFound("Không tìm thấy file mẫu Excel.");

            // ==== 3. Tạo workbook từ file mẫu ====
            using var workbook = new ClosedXML.Excel.XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1); // worksheet đầu tiên

            // ==== 4. Ghi dữ liệu bắt đầu từ dòng 6 ====
            int startRow = 3;
            int currentRow = startRow;

            foreach (var t in items)
            {
                ws.Cell(currentRow, 1).Value = currentRow -2;
                ws.Cell(currentRow, 2).Value = t.TicketCode;
                ws.Cell(currentRow, 3).Value = t.TicketTitle;
                ws.Cell(currentRow, 4).Value = t.TicketType;
                ws.Cell(currentRow, 5).Value = t.UserCode + "-"+t.UserName;
                ws.Cell(currentRow, 6).Value = t.UserDepartment;
                ws.Cell(currentRow, 7).Value = t.CreatedAt?.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(currentRow, 8).Value = t.UserAssigneeCode + "-" + t.UserAssigneeName;
                ws.Cell(currentRow, 9).Value = t.UserAssigneeDepartment;
                ws.Cell(currentRow, 10).Value = t.ReceivedAt?.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(currentRow, 11).Value = t.ApprovedAt?.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(currentRow, 12).Value = GetTicketStatusName(t.TicketStatus); 
                ws.Cell(currentRow, 13).Value = t.Note;
                currentRow++;
            }

            // ==== 5. Cập nhật thêm thông tin chung vào template (ví dụ tiêu đề) ====
            //ws.Cell("B2").Value = $"Báo cáo Ticket Logs - Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";

            // ==== 6. Trả file Excel về client ====
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"TicketLogs_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        private static string GetTicketStatusName(byte? status)
        {
            switch (status)
            {
                case 0: return "Chờ tiếp nhận";
                case 1: return "Đã tiếp nhận";
                case 2: return "Hoàn tất";
                case 3: return "Hủy";
                default: return "Không xác định";
            }
        }
    }
}
