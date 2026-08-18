using ClosedXML.Excel;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;
using support.server.Models.SLC;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SlcImportController : ControllerBase
{
    private readonly AppDbContext _context;
    public SlcImportController(AppDbContext context) => _context = context;

    [HttpGet("template")]
    public IActionResult DownloadTemplate()
    {
        var wb = new XLWorkbook();

        // Sheet 1: Projects
        var ws1 = wb.AddWorksheet("Projects");
        string[] h1 = ["code*", "name*", "software_code*", "description", "start_date", "end_date", "go_live_date", "version", "status(0-4)", "weight"];
        SetHeader(ws1, h1, XLColor.LightBlue);
        ws1.Cell(2, 1).Value = "PROJ-001"; ws1.Cell(2, 2).Value = "Dự án mẫu";
        ws1.Cell(2, 3).Value = "SW-001"; ws1.Cell(2, 4).Value = "Mô tả dự án";
        ws1.Cell(2, 5).Value = "2026-01-01"; ws1.Cell(2, 6).Value = "2026-06-30";
        ws1.Cell(2, 7).Value = "2026-07-01"; ws1.Cell(2, 8).Value = "1.0.0";
        ws1.Cell(2, 9).Value = 1; ws1.Cell(2, 10).Value = 1;
        ws1.Columns().AdjustToContents();

        // Sheet 2: Modules
        var ws2 = wb.AddWorksheet("Modules");
        string[] h2 = ["code*", "name*", "project_code*", "description", "assignee_code", "assignee_name", "start_date", "end_date", "planned_progress", "actual_progress", "weight", "status(0-3)", "version"];
        SetHeader(ws2, h2, XLColor.LightGreen);
        ws2.Cell(2, 1).Value = "MOD-001"; ws2.Cell(2, 2).Value = "Module phân tích";
        ws2.Cell(2, 3).Value = "PROJ-001"; ws2.Cell(2, 4).Value = "Mô tả module";
        ws2.Cell(2, 5).Value = "NV001"; ws2.Cell(2, 6).Value = "Nguyễn Văn A";
        ws2.Cell(2, 7).Value = "2026-01-01"; ws2.Cell(2, 8).Value = "2026-03-31";
        ws2.Cell(2, 9).Value = 0; ws2.Cell(2, 10).Value = 0;
        ws2.Cell(2, 11).Value = 1; ws2.Cell(2, 12).Value = 0; ws2.Cell(2, 13).Value = "1.0";
        ws2.Columns().AdjustToContents();

        // Sheet 3: Tasks
        var ws3 = wb.AddWorksheet("Tasks");
        string[] h3 = ["code*", "name*", "module_code*", "description", "assignee_code", "assignee_name", "start_date", "end_date", "priority(1-4)", "status(0-3)", "progress(0-100)", "estimated_hours"];
        SetHeader(ws3, h3, XLColor.LightYellow);
        ws3.Cell(2, 1).Value = "TASK-001"; ws3.Cell(2, 2).Value = "Phân tích yêu cầu";
        ws3.Cell(2, 3).Value = "MOD-001"; ws3.Cell(2, 4).Value = "Mô tả task";
        ws3.Cell(2, 5).Value = "NV001"; ws3.Cell(2, 6).Value = "Nguyễn Văn A";
        ws3.Cell(2, 7).Value = "2026-01-01"; ws3.Cell(2, 8).Value = "2026-01-15";
        ws3.Cell(2, 9).Value = 2; ws3.Cell(2, 10).Value = 0;
        ws3.Cell(2, 11).Value = 0; ws3.Cell(2, 12).Value = 8;
        ws3.Columns().AdjustToContents();

        // Sheet 4: ChangeRequests
        var ws4 = wb.AddWorksheet("ChangeRequests");
        string[] h4 = ["title*", "project_code", "module_code", "priority(1-4)", "requestor_code", "requestor_name", "requestor_dept", "content*", "reason", "impact_timeline(0/1)", "impact_days", "impact_version"];
        SetHeader(ws4, h4, XLColor.LightSalmon);
        ws4.Cell(2, 1).Value = "Thêm tính năng xuất báo cáo";
        ws4.Cell(2, 2).Value = "PROJ-001"; ws4.Cell(2, 3).Value = "MOD-001";
        ws4.Cell(2, 4).Value = 2; ws4.Cell(2, 5).Value = "NV001";
        ws4.Cell(2, 6).Value = "Nguyễn Văn A"; ws4.Cell(2, 7).Value = "Phòng CNTT";
        ws4.Cell(2, 8).Value = "Mô tả nội dung thay đổi";
        ws4.Cell(2, 9).Value = "Lý do thay đổi";
        ws4.Cell(2, 10).Value = 0; ws4.Cell(2, 11).Value = 0; ws4.Cell(2, 12).Value = "";
        ws4.Columns().AdjustToContents();

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();
        wb.Dispose();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "slc_import_template.xlsx");
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Không có file được gửi lên.");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) return BadRequest("Chỉ hỗ trợ file .xlsx");

        var result = new SlcImportResult();
        var now = DateTime.Now;

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        using var wb = new XLWorkbook(ms);

        // === Projects ===
        var wsProjects = wb.Worksheets.FirstOrDefault(ws => ws.Name.Equals("Projects", StringComparison.OrdinalIgnoreCase));
        if (wsProjects != null)
        {
            var rows = wsProjects.RowsUsed().Skip(1).ToList();
            var existingProjects = await _context.SlcProjects.ToDictionaryAsync(p => p.Code, StringComparer.OrdinalIgnoreCase);
            var existingSoftware = await _context.SoftwareCatalogs.ToDictionaryAsync(s => s.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var code = GetStr(row, 1);
                var name = GetStr(row, 2);
                var softwareCode = GetStr(row, 3);
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

                if (!existingSoftware.TryGetValue(softwareCode, out var software))
                {
                    if (string.IsNullOrWhiteSpace(softwareCode))
                    {
                        result.Errors.Add($"Projects/{code}: software_code bắt buộc.");
                        continue;
                    }
                    software = new SoftwareCatalog { Code = softwareCode, Name = softwareCode, Status = 1, CreatedAt = now };
                    _context.SoftwareCatalogs.Add(software);
                    await _context.SaveChangesAsync();
                    existingSoftware[software.Code] = software;
                    result.Warnings.Add($"Tự động tạo phần mềm '{softwareCode}' vì không tìm thấy trong danh mục.");
                }

                var startDate = TryParseDate(GetStr(row, 5));
                var endDate = TryParseDate(GetStr(row, 6));
                var goLiveDate = TryParseDate(GetStr(row, 7));
                var version = GetStr(row, 8);
                var status = (byte)TryParseInt(GetStr(row, 9), 0);
                var weight = (decimal)TryParseDouble(GetStr(row, 10), 1);
                var desc = GetStr(row, 4);

                if (existingProjects.TryGetValue(code, out var existing))
                {
                    existing.Name = name;
                    existing.SoftwareId = software.Id;
                    if (!string.IsNullOrWhiteSpace(desc)) existing.Description = desc;
                    if (startDate.HasValue) existing.StartDate = startDate;
                    if (endDate.HasValue) existing.EndDate = endDate;
                    if (goLiveDate.HasValue) existing.GoLiveDate = goLiveDate;
                    if (!string.IsNullOrWhiteSpace(version)) existing.Version = version;
                    existing.Status = status;
                    existing.Weight = weight;
                    result.ProjectsUpdated++;
                }
                else
                {
                    var p = new SlcProject
                    {
                        Code = code, Name = name, SoftwareId = software.Id,
                        Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
                        StartDate = startDate, EndDate = endDate, GoLiveDate = goLiveDate,
                        Version = string.IsNullOrWhiteSpace(version) ? null : version,
                        Status = status, Weight = weight, CreatedAt = now
                    };
                    _context.SlcProjects.Add(p);
                    await _context.SaveChangesAsync();
                    existingProjects[code] = p;
                    result.ProjectsAdded++;
                }
            }
            await _context.SaveChangesAsync();
        }

        // === Modules ===
        var wsModules = wb.Worksheets.FirstOrDefault(ws => ws.Name.Equals("Modules", StringComparison.OrdinalIgnoreCase));
        if (wsModules != null)
        {
            var rows = wsModules.RowsUsed().Skip(1).ToList();
            var existingModules = await _context.SlcModules.ToDictionaryAsync(m => m.Code, StringComparer.OrdinalIgnoreCase);
            var projects = await _context.SlcProjects.ToDictionaryAsync(p => p.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var code = GetStr(row, 1);
                var name = GetStr(row, 2);
                var projectCode = GetStr(row, 3);
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;
                if (!projects.TryGetValue(projectCode, out var project))
                {
                    result.Errors.Add($"Modules/{code}: Không tìm thấy project '{projectCode}'.");
                    continue;
                }

                var desc = GetStr(row, 4);
                var assigneeCode = GetStr(row, 5);
                var assigneeName = GetStr(row, 6);
                var startDate = TryParseDate(GetStr(row, 7));
                var endDate = TryParseDate(GetStr(row, 8));
                var planned = (decimal)TryParseDouble(GetStr(row, 9), 0);
                var actual = (decimal)TryParseDouble(GetStr(row, 10), 0);
                var weight = (decimal)TryParseDouble(GetStr(row, 11), 1);
                var status = (byte)TryParseInt(GetStr(row, 12), 0);
                var version = GetStr(row, 13);

                if (existingModules.TryGetValue(code, out var existing))
                {
                    existing.Name = name; existing.ProjectId = project.Id;
                    if (!string.IsNullOrWhiteSpace(desc)) existing.Description = desc;
                    if (!string.IsNullOrWhiteSpace(assigneeCode)) existing.AssigneeCode = assigneeCode;
                    if (!string.IsNullOrWhiteSpace(assigneeName)) existing.AssigneeName = assigneeName;
                    if (startDate.HasValue) existing.StartDate = startDate;
                    if (endDate.HasValue) existing.EndDate = endDate;
                    existing.PlannedProgress = planned; existing.ActualProgress = actual;
                    existing.Weight = weight; existing.Status = status;
                    if (!string.IsNullOrWhiteSpace(version)) existing.Version = version;
                    result.ModulesUpdated++;
                }
                else
                {
                    var m = new SlcModule
                    {
                        Code = code, Name = name, ProjectId = project.Id,
                        Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
                        AssigneeCode = string.IsNullOrWhiteSpace(assigneeCode) ? null : assigneeCode,
                        AssigneeName = string.IsNullOrWhiteSpace(assigneeName) ? null : assigneeName,
                        StartDate = startDate, EndDate = endDate,
                        PlannedProgress = planned, ActualProgress = actual,
                        Weight = weight, Status = status,
                        Version = string.IsNullOrWhiteSpace(version) ? null : version,
                        CreatedAt = now
                    };
                    _context.SlcModules.Add(m);
                    await _context.SaveChangesAsync();
                    existingModules[code] = m;
                    result.ModulesAdded++;
                }
            }
            await _context.SaveChangesAsync();
        }

        // === Tasks ===
        var wsTasks = wb.Worksheets.FirstOrDefault(ws => ws.Name.Equals("Tasks", StringComparison.OrdinalIgnoreCase));
        if (wsTasks != null)
        {
            var rows = wsTasks.RowsUsed().Skip(1).ToList();
            var existingTasks = await _context.SlcTasks.ToDictionaryAsync(t => t.Code, StringComparer.OrdinalIgnoreCase);
            var modules = await _context.SlcModules.ToDictionaryAsync(m => m.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var code = GetStr(row, 1);
                var name = GetStr(row, 2);
                var moduleCode = GetStr(row, 3);
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;
                if (!modules.TryGetValue(moduleCode, out var module))
                {
                    result.Errors.Add($"Tasks/{code}: Không tìm thấy module '{moduleCode}'.");
                    continue;
                }

                var desc = GetStr(row, 4);
                var assigneeCode = GetStr(row, 5);
                var assigneeName = GetStr(row, 6);
                var startDate = TryParseDate(GetStr(row, 7));
                var endDate = TryParseDate(GetStr(row, 8));
                var priority = (byte)TryParseInt(GetStr(row, 9), 2);
                var status = (byte)TryParseInt(GetStr(row, 10), 0);
                var progress = (decimal)TryParseDouble(GetStr(row, 11), 0);
                var ehStr = GetStr(row, 12);
                decimal? estimatedHours = string.IsNullOrWhiteSpace(ehStr) ? null : (decimal)TryParseDouble(ehStr, 0);

                if (existingTasks.TryGetValue(code, out var existing))
                {
                    existing.Name = name; existing.ModuleId = module.Id;
                    if (!string.IsNullOrWhiteSpace(desc)) existing.Description = desc;
                    if (!string.IsNullOrWhiteSpace(assigneeCode)) existing.AssigneeCode = assigneeCode;
                    if (!string.IsNullOrWhiteSpace(assigneeName)) existing.AssigneeName = assigneeName;
                    if (startDate.HasValue) existing.StartDate = startDate;
                    if (endDate.HasValue) existing.EndDate = endDate;
                    existing.Priority = priority; existing.Status = status; existing.Progress = progress;
                    if (estimatedHours.HasValue) existing.EstimatedHours = estimatedHours;
                    result.TasksUpdated++;
                }
                else
                {
                    var t = new SlcTask
                    {
                        Code = code, Name = name, ModuleId = module.Id,
                        Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
                        AssigneeCode = string.IsNullOrWhiteSpace(assigneeCode) ? null : assigneeCode,
                        AssigneeName = string.IsNullOrWhiteSpace(assigneeName) ? null : assigneeName,
                        StartDate = startDate, EndDate = endDate,
                        Priority = priority, Status = status, Progress = progress,
                        EstimatedHours = estimatedHours, CreatedAt = now
                    };
                    _context.SlcTasks.Add(t);
                    await _context.SaveChangesAsync();
                    existingTasks[code] = t;
                    result.TasksAdded++;
                }
            }
            await _context.SaveChangesAsync();
        }

        // === ChangeRequests ===
        var wsCR = wb.Worksheets.FirstOrDefault(ws => ws.Name.Equals("ChangeRequests", StringComparison.OrdinalIgnoreCase));
        if (wsCR != null)
        {
            await using var crTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var rows = wsCR.RowsUsed().Skip(1).ToList();
            var projects = await _context.SlcProjects.ToDictionaryAsync(p => p.Code, StringComparer.OrdinalIgnoreCase);
            var modules = await _context.SlcModules.ToDictionaryAsync(m => m.Code, StringComparer.OrdinalIgnoreCase);
            var todayStr = now.ToString("yyMMdd");
            var existingCodes = await _context.ChangeRequests
                .Where(cr => cr.Code.StartsWith($"REQ-{todayStr}-"))
                .Select(cr => cr.Code)
                .ToListAsync();
            var crSequence = existingCodes
                .Select(code => int.TryParse(code.Split('-').LastOrDefault(), out var number) ? number : 0)
                .DefaultIfEmpty(0)
                .Max();

            foreach (var row in rows)
            {
                var title = GetStr(row, 1);
                var content = GetStr(row, 8);
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content)) continue;

                var projectCode = GetStr(row, 2);
                var moduleCode = GetStr(row, 3);
                var priority = (byte)TryParseInt(GetStr(row, 4), 2);
                var requestorCode = GetStr(row, 5);
                var requestorName = GetStr(row, 6);
                var requestorDept = GetStr(row, 7);
                var reason = GetStr(row, 9);
                var impactTimeline = TryParseInt(GetStr(row, 10), 0) == 1;
                var impactDays = TryParseInt(GetStr(row, 11), 0);
                var impactVersion = GetStr(row, 12);

                projects.TryGetValue(projectCode, out var project);
                modules.TryGetValue(moduleCode, out var module);

                crSequence++;
                var code = $"REQ-{todayStr}-{crSequence:D4}";

                var cr = new ChangeRequest
                {
                    Code = code, Title = title, Content = content,
                    Reason = string.IsNullOrWhiteSpace(reason) ? null : reason,
                    Priority = priority, ProjectId = project?.Id, ModuleId = module?.Id,
                    RequestorCode = string.IsNullOrWhiteSpace(requestorCode) ? null : requestorCode,
                    RequestorName = string.IsNullOrWhiteSpace(requestorName) ? null : requestorName,
                    RequestorDept = string.IsNullOrWhiteSpace(requestorDept) ? null : requestorDept,
                    ImpactTimeline = impactTimeline, ImpactDays = impactDays,
                    ImpactVersion = string.IsNullOrWhiteSpace(impactVersion) ? null : impactVersion,
                    Status = 0, CurrentRevision = 0, CreatedAt = now
                };
                cr.Revisions.Add(new ChangeRevision
                {
                    RevisionNumber = 0,
                    Content = content,
                    Reason = string.IsNullOrWhiteSpace(reason) ? null : reason,
                    RequestorCode = string.IsNullOrWhiteSpace(requestorCode) ? null : requestorCode,
                    RequestorName = string.IsNullOrWhiteSpace(requestorName) ? null : requestorName,
                    ImpactTimeline = impactTimeline, ImpactDays = impactDays,
                    ImpactVersion = string.IsNullOrWhiteSpace(impactVersion) ? null : impactVersion,
                    Status = 0, CreatedAt = now
                });
                _context.ChangeRequests.Add(cr);
                await _context.SaveChangesAsync();
                result.ChangeRequestsAdded++;
            }

            await crTransaction.CommitAsync();
        }

        result.Success = result.Errors.Count == 0;
        return Ok(result);
    }

    private static string GetStr(IXLRow row, int col)
    {
        try
        {
            var cell = row.Cell(col);
            if (cell == null) return string.Empty;
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime().ToString("yyyy-MM-dd");
            return (cell.GetString() ?? string.Empty).Trim();
        }
        catch { return string.Empty; }
    }

    private static DateOnly? TryParseDate(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateOnly.TryParse(s, out var d)) return d;
        if (DateTime.TryParse(s, out var dt)) return DateOnly.FromDateTime(dt);
        return null;
    }

    private static int TryParseInt(string s, int def = 0) =>
        int.TryParse(s.Trim(), out var v) ? v : def;

    private static double TryParseDouble(string s, double def = 0) =>
        double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;

    private static void SetHeader(IXLWorksheet ws, string[] headers, XLColor color)
    {
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = color;
    }
}

public class SlcImportResult
{
    public bool Success { get; set; }
    public int ProjectsAdded { get; set; }
    public int ProjectsUpdated { get; set; }
    public int ModulesAdded { get; set; }
    public int ModulesUpdated { get; set; }
    public int TasksAdded { get; set; }
    public int TasksUpdated { get; set; }
    public int ChangeRequestsAdded { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
