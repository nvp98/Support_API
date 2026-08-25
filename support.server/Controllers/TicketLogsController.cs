using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;
using support.server.DTOs;
using support.server.Models;
using support.server.Services;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
        public class TicketLogsController : ControllerBase
        {
            private readonly AppDbContext _context;
        private readonly ITeamsNotificationService _teamsNotificationService;
        public TicketLogsController(
            AppDbContext context,
            ITeamsNotificationService teamsNotificationService)
        {
            _context = context;
            _teamsNotificationService = teamsNotificationService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetPagedList(
        int page = 1,
        int pageSize = 10,
        byte? status = null,
        string department = null,
        string type = null,
        string? subType = null,
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

            // Filter theo hạng mục hỗ trợ cấp 2
            if (!string.IsNullOrEmpty(subType))
                query = query.Where(t => t.TicketSubType == subType);

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
                t.TicketSubType,
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
                t.Note,
                t.CompletedNote,
                t.ProcessingMinutes,
                t.ErrorClassification,
                t.HandlerClassification
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
            var classificationError = TicketClassificationCatalog.Validate(
                ticket.TicketType,
                ticket.TicketSubType);
            if (classificationError != null)
                return BadRequest(new { message = classificationError });

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
            // Chặn ảnh Base64
            if (!string.IsNullOrWhiteSpace(ticket.TicketContent) &&
                ticket.TicketContent.Contains("data:image", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Ảnh chưa upload hoàn tất hoặc đang sử dụng Base64. Vui lòng chờ upload xong rồi lưu lại."
                });
            }


            ticket.TicketCode = newCode; // override client gửi lên
            ticket.TicketStatus = 0;
            ticket.CreatedAt = DateTime.Now;
            _context.TicketLogs.Add(ticket);
            await _context.SaveChangesAsync();
            await _teamsNotificationService.SendTicketCreatedAsync(ticket);
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

            var classificationError = TicketClassificationCatalog.Validate(
                model.TicketType,
                model.TicketSubType);
            if (classificationError != null)
                return BadRequest(new { message = classificationError });

            // Cập nhật thông tin ticket
            var parts = model.TicketCode.Split('-');
            if (parts.Length > 0)
            {
                parts[0] = model.TicketType;
                ticket.TicketCode = string.Join("-", parts);
            }
            ticket.TicketContent = model.TicketContent;
            ticket.TicketType = model.TicketType;
            ticket.TicketSubType = model.TicketSubType;
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
            if (ticket.TicketStatus == 0) // chỉ ở trạng thái chờ tiếp nhận mới được cập nhật
            {
                // Cập nhật các trường cần thiết
                ticket.TicketStatus = 1; // tiếp nhận
                ticket.UserAssigneeCode = model.UserAssigneeCode;
                ticket.UserAssigneeName = model.UserAssigneeName;
                ticket.UserAssigneeDepartment = model.UserAssigneeDepartment;
                ticket.ReceivedAt = DateTime.Now;
            }
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
            ticket.ApprovedAt = null;
            ticket.Note = null;
            ticket.CompletedNote = null;
            ticket.ProcessingMinutes = null;
            ticket.ErrorClassification = null;
            ticket.HandlerClassification = null;

            await _context.SaveChangesAsync();
            return Ok(ticket);
        }

        [HttpPut("completed/{id}")]
        public async Task<IActionResult> UpdateCompleted(int id, [FromBody] CompleteTicketRequest model)
        {
            var ticket = await _context.TicketLogs.FindAsync(id);
            if (ticket == null)
                return NotFound("Không tìm thấy ticket.");

            if (ticket.TicketStatus != 1)
                return Conflict(new { message = "Chỉ ticket đang xử lý mới có thể hoàn thành." });

            var completedNote = model.CompletedNote.Trim();
            if (!HasMeaningfulCompletedNote(completedNote))
                return BadRequest(new { message = "Ghi chú hoàn thành phải có nội dung hoặc hình ảnh hợp lệ." });

            if (completedNote.Length > 100_000)
                return BadRequest(new { message = "Ghi chú hoàn thành không được vượt quá 100.000 ký tự." });

            ticket.TicketStatus = 2;
            ticket.CompletedNote = completedNote;
            ticket.ProcessingMinutes = model.ProcessingMinutes;
            ticket.ErrorClassification = model.ErrorClassification;
            ticket.HandlerClassification = model.HandlerClassification;
            ticket.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(ticket);
        }

        [HttpPut("note/{id}")]
        public async Task<IActionResult> UpdateNote(int id, [FromBody] TicketLog model)
        {
            var ticket = await _context.TicketLogs.FindAsync(id);
            if (ticket == null)
                return NotFound("Không tìm thấy ticket.");
            //if(ticket.UserAssigneeCode != model.UserAssigneeCode)
            //    return NotFound("Ticket này chỉ được đóng với user đã tiếp nhận.");
            // Cập nhật trạng thái và thông tin hoàn tất
            ticket.Note = model.Note;                 // ghi chú kết quả xử lý

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
            ticket.UserAssigneeCode = model.UserAssigneeCode;
            ticket.UserAssigneeName = model.UserAssigneeName;
            ticket.UserAssigneeDepartment = model.UserAssigneeDepartment;
            ticket.ReceivedAt = DateTime.Now;         // thời điểm tiếp nhận / phê duyệt

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật hoàn tất ticket thành công.",
                ticket
            });
        }


        [HttpGet("Summary")]
        public async Task<ActionResult<object>> GetSummary()
        {
            var today = DateTime.Today;
            var fromDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            var toDate = today.AddDays(1);

            var tickets = await _context.TicketLogs
                .Where(t => t.CreatedAt >= fromDate && t.CreatedAt < toDate)
                .Select(t => new
                {
                    t.TicketType,
                    t.TicketStatus,
                    t.UserAssigneeCode,
                    t.UserAssigneeName
                })
                .ToListAsync();

            var waitingTickets = tickets.Where(t => t.TicketStatus == 1).ToList();

            var waitingByType = new Dictionary<string, int>
            {
                ["SOFT"] = waitingTickets.Count(t => t.TicketType == "SOFT"),
                ["HARD"] = waitingTickets.Count(t => t.TicketType == "HARD"),
                ["SAP"] = waitingTickets.Count(t => t.TicketType == "SAP"),
            };

            // Load staff config
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "configs", "json_info_user.json");
            var staffList = new List<(string Code, string Name, string Email, string Group, string? Avatar)>();
            if (System.IO.File.Exists(configPath))
            {
                var json = await System.IO.File.ReadAllTextAsync(configPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("supportStaff", out var arr))
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        staffList.Add((
                            Code: item.GetProperty("maNhanVien").GetString() ?? "",
                            Name: item.GetProperty("hoTen").GetString() ?? "",
                            Email: item.GetProperty("email").GetString() ?? "",
                            Group: item.GetProperty("maNhomHoTro").GetString() ?? "",
                            Avatar: item.TryGetProperty("avatar", out var av) ? av.GetString() : null
                        ));
                    }
                }
            }

            var staffMap = staffList.ToDictionary(s => s.Code);

            // Init each group with all staff at count 0
            var groups = new[] { "SOFT", "HARD", "SAP" };
            var summaryByGroup = groups.ToDictionary(
                g => g,
                g => staffList
                    .Where(s => s.Group == g)
                    .Select(s => new StaffCount { Code = s.Code, Name = s.Name, Email = s.Email, Avatar = s.Avatar, Count = 0 })
                    .ToList()
            );

            // Accumulate counts from waiting tickets
            foreach (var t in waitingTickets.Where(t => !string.IsNullOrEmpty(t.UserAssigneeCode)))
            {
                var code = t.UserAssigneeCode!;
                var groupKey = staffMap.TryGetValue(code, out var info) ? info.Group : "SOFT";
                var list = summaryByGroup[groupKey];
                var entry = list.FirstOrDefault(x => x.Code == code);
                if (entry == null)
                {
                    entry = new StaffCount { Code = code, Name = t.UserAssigneeName ?? "Chưa rõ", Email = info.Email, Avatar = info.Avatar, Count = 0 };
                    list.Add(entry);
                }
                entry.Count += 1;
            }

            var todaySupportSummary = groups.ToDictionary(
                g => g,
                g => summaryByGroup[g].OrderByDescending(x => x.Count).ToList()
            );

            return Ok(new { waitingByType, todaySupportSummary });
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
    string? subType = null,
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

            if (!string.IsNullOrEmpty(subType))
                query = query.Where(t => t.TicketSubType == subType);

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

            ws.Cell(startRow - 1, 14).Value = "Thời gian xử lý (phút)";
            ws.Cell(startRow - 1, 15).Value = "Phân loại lỗi";
            ws.Cell(startRow - 1, 16).Value = "Phân loại xử lý";
            ws.Cell(startRow - 1, 17).Value = "Ghi chú hoàn thành";
            ws.Cell(startRow - 1, 18).Value = "Hạng mục hỗ trợ";

            foreach (var t in items)
            {
                ws.Cell(currentRow, 1).Value = currentRow - 2;
                ws.Cell(currentRow, 2).Value = t.TicketCode;
                ws.Cell(currentRow, 3).Value = t.TicketTitle;
                ws.Cell(currentRow, 4).Value = t.TicketType;
                ws.Cell(currentRow, 5).Value = t.UserCode + "-" + t.UserName;
                ws.Cell(currentRow, 6).Value = t.UserDepartment;
                ws.Cell(currentRow, 7).Value = t.CreatedAt?.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(currentRow, 8).Value = t.UserAssigneeCode + "-" + t.UserAssigneeName;
                ws.Cell(currentRow, 9).Value = t.UserAssigneeDepartment;
                ws.Cell(currentRow, 10).Value = t.ReceivedAt?.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(currentRow, 11).Value = t.ApprovedAt?.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(currentRow, 12).Value = GetTicketStatusName(t.TicketStatus);
                ws.Cell(currentRow, 13).Value = t.Note;
                ws.Cell(currentRow, 14).Value = t.ProcessingMinutes;
                ws.Cell(currentRow, 15).Value = GetErrorClassificationName(t.ErrorClassification);
                ws.Cell(currentRow, 16).Value = GetHandlerClassificationName(t.HandlerClassification);
                ws.Cell(currentRow, 17).Value = GetCompletedNoteForExport(t.CompletedNote);
                ws.Cell(currentRow, 18).Value = TicketClassificationCatalog.GetSubTypeLabel(t.TicketSubType);
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

        private static bool HasMeaningfulCompletedNote(string html)
        {
            var text = WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
            return !string.IsNullOrWhiteSpace(text) ||
                   Regex.IsMatch(html, "<img\\b", RegexOptions.IgnoreCase);
        }

        private static string GetErrorClassificationName(string? classification) => classification switch
        {
            "OLD" => "Lỗi cũ (đã từng xảy ra)",
            "NEW" => "Lỗi mới (lần đầu phát sinh)",
            _ => string.Empty
        };

        private static string GetHandlerClassificationName(string? classification) => classification switch
        {
            "IT" => "IT xử lý",
            "NT" => "NT xử lý",
            _ => string.Empty
        };

        private static string GetCompletedNoteForExport(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var text = WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
            text = Regex.Replace(text, "\\s+", " ").Trim();

            var imageUrls = Regex.Matches(
                    html,
                    "<img[^>]+src=[\\\"'](?<url>[^\\\"']+)[\\\"']",
                    RegexOptions.IgnoreCase)
                .Select(match => match.Groups["url"].Value)
                .Distinct();
            var images = string.Join("; ", imageUrls);

            return string.IsNullOrEmpty(images) ? text : $"{text} Hình ảnh: {images}".Trim();
        }

        private class StaffCount
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public string? Avatar { get; set; }
            public int Count { get; set; }
        }
    }
}
