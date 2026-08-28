using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;
using support.server.Models.SLC;
using support.server.Services;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChangeRequestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITeamsNotificationService _teamsNotificationService;

    public ChangeRequestController(
        AppDbContext context,
        ITeamsNotificationService teamsNotificationService)
    {
        _context = context;
        _teamsNotificationService = teamsNotificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? actorCode,
        [FromQuery] int? projectId,
        [FromQuery] int? moduleId,
        [FromQuery] byte? status,
        [FromQuery] byte? priority,
        [FromQuery] string? requestorCode,
        [FromQuery] bool myRequest,
        [FromQuery] string? keyword,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var rolesResult = await GetActorRolesAsync(actorCode);
        if (rolesResult.Error != null) return rolesResult.Error;
        if (page < 1 || pageSize < 1) return BadRequest(new { message = "Trang và số bản ghi mỗi trang phải lớn hơn 0." });

        var query = _context.ChangeRequests
            .Include(cr => cr.Module)
            .Include(cr => cr.Project)
            .AsNoTracking()
            .AsQueryable();

        if (projectId.HasValue) query = query.Where(cr => cr.ProjectId == projectId.Value);
        if (moduleId.HasValue) query = query.Where(cr => cr.ModuleId == moduleId.Value);
        if (status.HasValue) query = query.Where(cr => cr.Status == status.Value);
        if (priority.HasValue) query = query.Where(cr => cr.Priority == priority.Value);
        if (!string.IsNullOrWhiteSpace(requestorCode)) query = query.Where(cr => cr.RequestorCode == requestorCode);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(cr => cr.Title.Contains(keyword) || cr.Code.Contains(keyword));
        if (fromDate.HasValue) query = query.Where(cr => cr.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(cr => cr.CreatedAt <= toDate.Value);

        if (myRequest)
        {
            query = query.Where(cr => cr.CreatedByCode == rolesResult.ActorCode
                || cr.RequestorCode == rolesResult.ActorCode
                || cr.DeveloperCode == rolesResult.ActorCode);
        }
        else if (!rolesResult.Roles.Contains(ChangeRequestWorkflow.AdminRole))
        {
            query = query.Where(cr => cr.CreatedByCode == rolesResult.ActorCode
                || cr.RequestorCode == rolesResult.ActorCode);
        }

        var total = await query.CountAsync();
        var entities = await query
            .OrderByDescending(cr => cr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(cr => new
            {
                cr.Id, cr.Code, cr.Title, cr.Priority, cr.Status,
                cr.FileAttachments,
                cr.ModuleId, ModuleName = cr.Module != null ? cr.Module.Name : null,
                cr.ProjectId, ProjectName = cr.Project != null ? cr.Project.Name : null,
                cr.RequestorCode, cr.RequestorName, cr.RequestorDept,
                cr.CreatedByCode, cr.CreatedByName,
                cr.ApproverCode, cr.ApproverName,
                cr.DeveloperCode, cr.DeveloperName, cr.DeveloperAcceptedAt, cr.ExpectedCompletionDate,
                cr.IsDeployed, cr.IsChecked,
                cr.SubmittedAt, cr.SubmittedByCode, cr.SubmittedByName,
                cr.ImpactTimeline, cr.ImpactDays, cr.ImpactVersion,
                cr.CurrentRevision, cr.CreatedAt, cr.ApprovedAt, cr.CompletedAt,
                cr.CompletedByCode, cr.CompletedByName, cr.RejectedAt, cr.RejectedReason,
                RevisionCount = cr.Revisions.Count()
            })
            .ToListAsync();

        var items = entities.Select(cr => new
        {
            cr.Id, cr.Code, cr.Title, cr.Priority, cr.Status,
            cr.FileAttachments,
            StatusName = ChangeRequestWorkflow.GetStatusName(cr.Status),
            cr.ModuleId, cr.ModuleName, cr.ProjectId, cr.ProjectName,
            cr.RequestorCode, cr.RequestorName, cr.RequestorDept,
            cr.CreatedByCode, cr.CreatedByName,
            cr.ApproverCode, cr.ApproverName, cr.DeveloperCode, cr.DeveloperName,
            cr.DeveloperAcceptedAt, cr.ExpectedCompletionDate,
            cr.IsDeployed, cr.IsChecked,
            cr.SubmittedAt, cr.SubmittedByCode, cr.SubmittedByName,
            cr.ImpactTimeline, cr.ImpactDays, cr.ImpactVersion,
            cr.CurrentRevision, cr.CreatedAt, cr.ApprovedAt, cr.CompletedAt,
            cr.CompletedByCode, cr.CompletedByName, cr.RejectedAt, cr.RejectedReason,
            cr.RevisionCount,
            AllowedActions = ChangeRequestWorkflow.GetAllowedActions(
                cr.Status, rolesResult.Roles, rolesResult.ActorCode, cr.CreatedByCode,
                cr.RequestorCode, cr.DeveloperCode)
        });

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] string? actorCode)
    {
        var rolesResult = await GetActorRolesAsync(actorCode);
        if (rolesResult.Error != null) return rolesResult.Error;

        var item = await _context.ChangeRequests
            .Include(cr => cr.Module)
            .Include(cr => cr.Project)
            .Include(cr => cr.Revisions.OrderBy(r => r.RevisionNumber))
            .AsNoTracking()
            .FirstOrDefaultAsync(cr => cr.Id == id);
        if (item == null) return NotFound(new { message = "Không tìm thấy Change Request." });
        if (!rolesResult.Roles.Contains(ChangeRequestWorkflow.AdminRole)
            && !string.Equals(item.CreatedByCode, rolesResult.ActorCode, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(item.RequestorCode, rolesResult.ActorCode, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(item.DeveloperCode, rolesResult.ActorCode, StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Bạn không có quyền xem Change Request này." });

        item.AllowedActions = ChangeRequestWorkflow.GetAllowedActions(
            item.Status, rolesResult.Roles, rolesResult.ActorCode, item.CreatedByCode,
            item.RequestorCode, item.DeveloperCode);
        return Ok(item);
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Create([FromForm] ChangeRequestMutationDto dto)
    {
        var actorError = ValidateActor(dto);
        if (actorError != null) return actorError;
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest(new { message = "Tiêu đề là bắt buộc." });
        if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest(new { message = "Nội dung là bắt buộc." });
        if (dto.Content.Contains("data:image", StringComparison.OrdinalIgnoreCase)
            || dto.Content.Contains("blob:", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Nội dung thay đổi chứa ảnh chưa được upload. Vui lòng chờ ảnh upload hoàn tất rồi lưu lại." });
        if (!string.IsNullOrWhiteSpace(dto.BeforeChangeContent)
            && (dto.BeforeChangeContent.Contains("data:image", StringComparison.OrdinalIgnoreCase)
                || dto.BeforeChangeContent.Contains("blob:", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { message = "Nội dung trước thay đổi chứa ảnh chưa được upload. Vui lòng chờ ảnh upload hoàn tất rồi lưu lại." });
        if (dto.Priority is < 1 or > 4) return BadRequest(new { message = "Độ ưu tiên phải nằm trong khoảng 1 đến 4." });
        var savedFileName = await SaveAttachmentAsync(dto.UploadedFile);

        var now = DateTime.Now;
        var todayStr = now.ToString("yyMMdd");
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var codesToday = await _context.ChangeRequests
            .Where(cr => cr.Code.StartsWith($"REQ-{todayStr}-"))
            .Select(cr => cr.Code)
            .ToListAsync();
        var nextNumber = codesToday
            .Select(code => int.TryParse(code.Split('-').LastOrDefault(), out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
        var item = new ChangeRequest
        {
            Code = $"REQ-{todayStr}-{nextNumber:D4}",
            Title = dto.Title.Trim(),
            Content = dto.Content,
            BeforeChangeContent = dto.BeforeChangeContent,
            Reason = dto.Reason,
            FileAttachments = savedFileName,
            Priority = dto.Priority,
            ModuleId = dto.ModuleId,
            ProjectId = dto.ProjectId,
            RequestorCode = dto.RequestorCode,
            RequestorName = dto.RequestorName,
            RequestorDept = dto.RequestorDept,
            CreatedByCode = dto.ActorCode,
            CreatedByName = dto.ActorName,
            ImpactTimeline = dto.ImpactTimeline,
            ImpactDays = dto.ImpactDays,
            ImpactVersion = dto.ImpactVersion,
            ImpactModules = dto.ImpactModules,
            Status = (byte)ChangeRequestStatus.WaitingAcceptance,
            CurrentRevision = 0,
            CreatedAt = now,
            SubmittedAt = now,
            SubmittedByCode = dto.ActorCode,
            SubmittedByName = dto.ActorName
        };

        item.Revisions.Add(new ChangeRevision
        {
            RevisionNumber = 0,
            Content = item.Content,
            Reason = item.Reason,
            RequestorCode = item.RequestorCode,
            RequestorName = item.RequestorName,
            ImpactTimeline = item.ImpactTimeline,
            ImpactDays = item.ImpactDays,
            ImpactVersion = item.ImpactVersion,
            Status = 0,
            CreatedAt = now
        });
        _context.ChangeRequests.Add(item);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var softwareCode = item.ProjectId.HasValue
            ? await _context.SlcProjects
                .AsNoTracking()
                .Where(project => project.Id == item.ProjectId.Value)
                .Select(project => project.Software != null
                    ? project.Software.Code
                    : null)
                .FirstOrDefaultAsync()
            : null;
        await _teamsNotificationService.SendChangeRequestCreatedAsync(item, softwareCode);

        var roles = await LoadRolesAsync(dto.ActorCode);
        item.AllowedActions = ChangeRequestWorkflow.GetAllowedActions(
            item.Status, roles, dto.ActorCode, item.CreatedByCode,
            item.RequestorCode, item.DeveloperCode);
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Update(int id, [FromForm] ChangeRequestMutationDto dto)
    {
        var actorError = ValidateActor(dto);
        if (actorError != null) return actorError;

        var item = await _context.ChangeRequests.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy Change Request." });
        if (item.Status > (byte)ChangeRequestStatus.WaitingAcceptance) return WorkflowConflict();
        if (!await IsAdminOrCreatorAsync(dto.ActorCode, item.CreatedByCode))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Bạn chỉ được sửa Change Request do mình tạo trước khi phiếu được tiếp nhận." });
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest(new { message = "Tiêu đề là bắt buộc." });
        if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest(new { message = "Nội dung là bắt buộc." });
        if (dto.Content.Contains("data:image", StringComparison.OrdinalIgnoreCase)
            || dto.Content.Contains("blob:", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Nội dung thay đổi chứa ảnh chưa được upload. Vui lòng chờ ảnh upload hoàn tất rồi lưu lại." });
        if (!string.IsNullOrWhiteSpace(dto.BeforeChangeContent)
            && (dto.BeforeChangeContent.Contains("data:image", StringComparison.OrdinalIgnoreCase)
                || dto.BeforeChangeContent.Contains("blob:", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { message = "Nội dung trước thay đổi chứa ảnh chưa được upload. Vui lòng chờ ảnh upload hoàn tất rồi lưu lại." });
        if (dto.Priority is < 1 or > 4) return BadRequest(new { message = "Độ ưu tiên phải nằm trong khoảng 1 đến 4." });
        item.Title = dto.Title.Trim();
        item.Content = dto.Content;
        item.BeforeChangeContent = dto.BeforeChangeContent;
        item.Reason = dto.Reason;
        if (dto.UploadedFile != null)
        {
            item.FileAttachments = await SaveAttachmentAsync(dto.UploadedFile);
        }
        item.Priority = dto.Priority;
        item.ModuleId = dto.ModuleId;
        item.ProjectId = dto.ProjectId;
        item.RequestorCode = dto.RequestorCode;
        item.RequestorName = dto.RequestorName;
        item.RequestorDept = dto.RequestorDept;
        item.ImpactTimeline = dto.ImpactTimeline;
        item.ImpactDays = dto.ImpactDays;
        item.ImpactVersion = dto.ImpactVersion;
        item.ImpactModules = dto.ImpactModules;

        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return WorkflowConflict(); }
        return Ok(item);
    }

    [HttpPut("{id:int}/status")]
    public IActionResult UpdateStatus(int id) => StatusCode(StatusCodes.Status410Gone,
        new { message = "API đổi trạng thái tự do đã bị vô hiệu hóa. Hãy sử dụng command workflow tương ứng." });

    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id, [FromBody] AcceptChangeRequestDto dto)
    {
        var roleError = await RequireRoleAsync(dto.ActorCode, ChangeRequestWorkflow.DeveloperRole);
        if (roleError != null) return roleError;
        if (!dto.ExpectedCompletionDate.HasValue)
            return BadRequest(new { message = "Ngày hoàn thành dự kiến là bắt buộc." });
        if (dto.ExpectedCompletionDate.Value < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new { message = "Ngày hoàn thành dự kiến không được trước ngày hiện tại." });

        var item = await _context.ChangeRequests.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy Change Request." });
        if (item.Status != (byte)ChangeRequestStatus.WaitingAcceptance) return WorkflowConflict();

        item.DeveloperCode = dto.ActorCode;
        item.DeveloperName = dto.ActorName;
        item.DeveloperAcceptedAt = DateTime.Now;
        item.ExpectedCompletionDate = dto.ExpectedCompletionDate;
        item.Status = (byte)ChangeRequestStatus.WaitingApproval;
        return await SaveCommandAsync(item);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ActorCommandDto dto)
    {
        var roleError = await RequireRoleAsync(dto.ActorCode, ChangeRequestWorkflow.ApproverRole);
        if (roleError != null) return roleError;
        var item = await _context.ChangeRequests.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy Change Request." });
        if (item.Status != (byte)ChangeRequestStatus.WaitingApproval) return WorkflowConflict();

        item.ApproverCode = dto.ActorCode;
        item.ApproverName = dto.ActorName;
        item.ApprovedAt = DateTime.Now;
        item.Status = (byte)ChangeRequestStatus.WaitingCompletion;
        return await SaveCommandAsync(item);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectChangeRequestDto dto)
    {
        var roleError = await RequireRoleAsync(dto.ActorCode, ChangeRequestWorkflow.ApproverRole);
        if (roleError != null) return roleError;
        if (string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest(new { message = "Lý do từ chối là bắt buộc." });
        if (dto.Reason.Trim().Length > 500)
            return BadRequest(new { message = "Lý do từ chối không được vượt quá 500 ký tự." });

        var item = await _context.ChangeRequests.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy Change Request." });
        if (item.Status != (byte)ChangeRequestStatus.WaitingApproval) return WorkflowConflict();

        item.ApproverCode = dto.ActorCode;
        item.ApproverName = dto.ActorName;
        item.RejectedAt = DateTime.Now;
        item.RejectedReason = dto.Reason.Trim();
        item.Status = (byte)ChangeRequestStatus.Rejected;
        return await SaveCommandAsync(item);
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] ActorCommandDto dto)
    {
        var roleError = await RequireRoleAsync(dto.ActorCode, ChangeRequestWorkflow.DeveloperRole);
        if (roleError != null) return roleError;

        var item = await _context.ChangeRequests.FirstOrDefaultAsync(cr => cr.Id == id);
        if (item == null) return NotFound(new { message = "Không tìm thấy Change Request." });
        if (item.Status != (byte)ChangeRequestStatus.WaitingCompletion) return WorkflowConflict();

        var now = DateTime.Now;
        item.Status = (byte)ChangeRequestStatus.Completed;
        item.CompletedAt = now;
        item.CompletedByCode = dto.ActorCode;
        item.CompletedByName = dto.ActorName;

        if (item.ImpactTimeline && item.ImpactDays != 0 && item.ModuleId.HasValue)
        {
            var module = await _context.SlcModules.FindAsync(item.ModuleId.Value);
            if (module?.EndDate != null)
            {
                var oldEnd = module.EndDate.Value;
                var newEnd = oldEnd.AddDays(item.ImpactDays);
                module.EndDate = newEnd;
                _context.TimelineAdjustments.Add(new TimelineAdjustment
                {
                    EntityType = "Module",
                    EntityId = module.Id,
                    PreviousEndDate = oldEnd,
                    NewEndDate = newEnd,
                    AdjustmentDays = item.ImpactDays,
                    Reason = $"Change Request {item.Code} hoàn thành",
                    ChangeRequestId = item.Id,
                    ChangedBy = dto.ActorCode,
                    ChangedByName = dto.ActorName,
                    ChangedAt = now
                });
            }
        }

        return await SaveCommandAsync(item);
    }

    [HttpPost("{id:int}/revisions")]
    public async Task<IActionResult> AddRevision(int id, [FromBody] AddChangeRevisionDto dto)
    {
        var roleError = await RequireRoleAsync(dto.ActorCode, ChangeRequestWorkflow.AdminRole);
        if (roleError != null) return roleError;

        var cr = await _context.ChangeRequests.Include(x => x.Revisions).FirstOrDefaultAsync(x => x.Id == id);
        if (cr == null) return NotFound(new { message = "Không tìm thấy Change Request." });
        if (cr.Status > (byte)ChangeRequestStatus.WaitingCompletion) return WorkflowConflict();
        if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest(new { message = "Nội dung revision là bắt buộc." });

        var nextRevision = cr.Revisions.Any() ? cr.Revisions.Max(r => r.RevisionNumber) + 1 : 1;
        var revision = new ChangeRevision
        {
            ChangeRequestId = id,
            RevisionNumber = nextRevision,
            Content = dto.Content.Trim(),
            Reason = dto.Reason,
            RequestorCode = dto.RequestorCode,
            RequestorName = dto.RequestorName,
            ImpactAssessment = dto.ImpactAssessment,
            ImpactTimeline = dto.ImpactTimeline,
            ImpactDays = dto.ImpactDays,
            ImpactVersion = dto.ImpactVersion,
            Status = 0,
            CreatedAt = DateTime.Now
        };
        _context.ChangeRevisions.Add(revision);
        cr.CurrentRevision = nextRevision;
        cr.Content = revision.Content;

        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return WorkflowConflict(); }
        return Ok(revision);
    }

    [HttpPut("{id:int}/mark-deployed")]
    public async Task<IActionResult> MarkAsDeployed(
        int id,
        [FromBody] CompletionMarkerDto dto)
    {
        var roleError = await RequireRoleAsync(dto.ActorCode, ChangeRequestWorkflow.DeveloperRole);
        if (roleError != null) return roleError;

        var item = await _context.ChangeRequests.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy Change Request." });
        if (!CanChangeCompletionMarker(item.Status)) return CompletionMarkerConflict();
        if (!string.Equals(item.DeveloperCode, dto.ActorCode, StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Chỉ người phụ trách đã tiếp nhận CR mới được đánh dấu đã triển khai." });

        item.IsDeployed = dto.IsMarked;
        return await SaveCommandAsync(item);
    }

    [HttpPut("{id:int}/mark-checked")]
    public async Task<IActionResult> MarkAsChecked(
        int id,
        [FromBody] CompletionMarkerDto dto)
    {
        var actorError = ValidateActor(dto);
        if (actorError != null) return actorError;

        var item = await _context.ChangeRequests.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy Change Request." });
        if (!CanChangeCompletionMarker(item.Status)) return CompletionMarkerConflict();

        var isCreator = string.Equals(item.CreatedByCode, dto.ActorCode, StringComparison.OrdinalIgnoreCase);
        var isRequestor = string.Equals(item.RequestorCode, dto.ActorCode, StringComparison.OrdinalIgnoreCase);
        if (!isCreator && !isRequestor)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Chỉ người tạo hoặc người yêu cầu mới được đánh dấu đã kiểm tra." });

        item.IsChecked = dto.IsMarked;
        return await SaveCommandAsync(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromBody] ActorCommandDto dto)
    {
        var actorError = ValidateActor(dto);
        if (actorError != null) return actorError;

        var item = await _context.ChangeRequests.Include(cr => cr.Revisions).FirstOrDefaultAsync(cr => cr.Id == id);
        if (item == null) return NotFound(new { message = "Không tìm thấy Change Request." });
        if (item.Status > (byte)ChangeRequestStatus.WaitingAcceptance) return WorkflowConflict();
        if (!await IsAdminOrCreatorAsync(dto.ActorCode, item.CreatedByCode))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Bạn chỉ được xóa Change Request do mình tạo trước khi phiếu được tiếp nhận." });

        _context.ChangeRevisions.RemoveRange(item.Revisions);
        _context.ChangeRequests.Remove(item);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return WorkflowConflict(); }
        return Ok();
    }

    private async Task<IActionResult> SaveCommandAsync(ChangeRequest item)
    {
        try
        {
            await _context.SaveChangesAsync();
            return Ok(item);
        }
        catch (DbUpdateConcurrencyException)
        {
            return WorkflowConflict();
        }
    }

    private async Task<IActionResult?> RequireRoleAsync(string? actorCode, string role)
    {
        if (string.IsNullOrWhiteSpace(actorCode))
            return BadRequest(new { message = "actorCode là bắt buộc." });

        var roles = await LoadRolesAsync(actorCode);
        return roles.Contains(role) ? null : StatusCode(StatusCodes.Status403Forbidden,
            new { message = "Bạn không có quyền thực hiện thao tác này." });
    }

    private async Task<(string ActorCode, HashSet<string> Roles, IActionResult? Error)> GetActorRolesAsync(
        string? actorCode)
    {
        if (string.IsNullOrWhiteSpace(actorCode))
            return (string.Empty, [], BadRequest(new { message = "actorCode là bắt buộc." }));

        return (actorCode, await LoadRolesAsync(actorCode), null);
    }

    private async Task<HashSet<string>> LoadRolesAsync(string? actorCode)
    {
        if (string.IsNullOrWhiteSpace(actorCode)) return [];

        var roles = await _context.ChangeRequestRoles.AsNoTracking()
            .Where(x => x.IsActive && x.UserCode == actorCode)
            .Select(x => x.RoleCode)
            .Distinct()
            .ToListAsync();
        return roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> IsAdminOrCreatorAsync(string? actorCode, string? createdByCode)
    {
        if (!string.IsNullOrWhiteSpace(createdByCode)
            && string.Equals(actorCode, createdByCode, StringComparison.OrdinalIgnoreCase))
            return true;

        var roles = await LoadRolesAsync(actorCode);
        return roles.Contains(ChangeRequestWorkflow.AdminRole);
    }

    private IActionResult? ValidateActor(ActorCommandDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ActorCode))
            return BadRequest(new { message = "actorCode là bắt buộc." });
        if (dto.ActorCode.Length > 20)
            return BadRequest(new { message = "actorCode không được vượt quá 20 ký tự." });
        if (!string.IsNullOrWhiteSpace(dto.ActorName) && dto.ActorName.Length > 100)
            return BadRequest(new { message = "actorName không được vượt quá 100 ký tự." });
        return null;
    }

    private static bool CanChangeCompletionMarker(byte status) =>
        status != (byte)ChangeRequestStatus.WaitingAcceptance;

    private ObjectResult CompletionMarkerConflict() => Conflict(new
    {
        status = StatusCodes.Status409Conflict,
        message = "Không thể đánh dấu khi CR đang ở trạng thái chờ tiếp nhận."
    });

    private static async Task<string?> SaveAttachmentAsync(IFormFile? file)
    {
        if (file == null) return null;

        var savedFileName = $"{Guid.NewGuid()}_{file.FileName}";
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, savedFileName);
        await using var stream = new FileStream(filePath, FileMode.CreateNew);
        await file.CopyToAsync(stream);
        return savedFileName;
    }

    private ObjectResult WorkflowConflict() => Conflict(new
    {
        status = StatusCodes.Status409Conflict,
        message = "Change Request đã được xử lý hoặc không còn ở trạng thái phù hợp. Vui lòng tải lại dữ liệu."
    });
}

public class ActorCommandDto
{
    [Required]
    [MaxLength(20)]
    public string ActorCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ActorName { get; set; }
}

public class AcceptChangeRequestDto : ActorCommandDto
{
    public DateOnly? ExpectedCompletionDate { get; set; }
}

public class RejectChangeRequestDto : ActorCommandDto
{
    public string? Reason { get; set; }
}

public class ChangeRequestMutationDto : ActorCommandDto
{
    public IFormFile? UploadedFile { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? BeforeChangeContent { get; set; }
    public string? Reason { get; set; }
    public byte Priority { get; set; } = 2;
    public int? ModuleId { get; set; }
    public int? ProjectId { get; set; }
    public string? RequestorCode { get; set; }
    public string? RequestorName { get; set; }
    public string? RequestorDept { get; set; }
    public bool ImpactTimeline { get; set; }
    public int ImpactDays { get; set; }
    public string? ImpactVersion { get; set; }
    public string? ImpactModules { get; set; }
}

public class CompletionMarkerDto : ActorCommandDto
{
    public bool IsMarked { get; set; }
}

public class AddChangeRevisionDto : ActorCommandDto
{
    public string? Content { get; set; }
    public string? Reason { get; set; }
    public string? RequestorCode { get; set; }
    public string? RequestorName { get; set; }
    public string? ImpactAssessment { get; set; }
    public bool ImpactTimeline { get; set; }
    public int ImpactDays { get; set; }
    public string? ImpactVersion { get; set; }
}
