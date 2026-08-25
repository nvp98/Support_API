using System.Net.Http.Json;
using support.server.Models;
using support.server.Models.SLC;

namespace support.server.Services;

public interface ITeamsNotificationService
{
    Task<bool> SendChangeRequestCreatedAsync(ChangeRequest changeRequest, string? softwareCode);
    Task<bool> SendTicketCreatedAsync(TicketLog ticket);
}

public sealed class TeamsNotificationService : ITeamsNotificationService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TeamsNotificationService> _logger;

    public TeamsNotificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TeamsNotificationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendChangeRequestCreatedAsync(
        ChangeRequest changeRequest,
        string? softwareCode)
    {
        var softwareRoute = string.IsNullOrWhiteSpace(softwareCode)
            ? null
            : _configuration
                .GetSection("Teams:ChangeRequestSoftwareRoutes")
                .GetChildren()
                .FirstOrDefault(route => string.Equals(
                    route["SoftwareCode"],
                    softwareCode,
                    StringComparison.OrdinalIgnoreCase));
        var workflowUrl = softwareRoute is null
            ? _configuration["Teams:ChangeRequestWorkflowUrl"]
            : softwareRoute["WorkflowUrl"];

        if (!Uri.TryCreate(workflowUrl, UriKind.Absolute, out var workflowUri)
            || workflowUri.Scheme != Uri.UriSchemeHttps)
        {
            _logger.LogWarning(
                "Không gửi thông báo Teams cho Change Request {ChangeRequestCode}: Teams:ChangeRequestWorkflowUrl chưa được cấu hình hợp lệ.",
                changeRequest.Code);
            return false;
        }

        var pageUrl = _configuration["Teams:ChangeRequestPageUrl"];
        var card = BuildChangeRequestCard(changeRequest, pageUrl);
        return await SendAsync(workflowUri, card, "Change Request", changeRequest.Code);
    }

    public async Task<bool> SendTicketCreatedAsync(TicketLog ticket)
    {
        var workflowUrl = _configuration["Teams:TicketWorkflowUrl"];
        if (!Uri.TryCreate(workflowUrl, UriKind.Absolute, out var workflowUri)
            || workflowUri.Scheme != Uri.UriSchemeHttps)
        {
            _logger.LogWarning(
                "Không gửi thông báo Teams cho Ticket {TicketCode}: Teams:TicketWorkflowUrl chưa được cấu hình hợp lệ.",
                ticket.TicketCode);
            return false;
        }

        var pageUrl = _configuration["Teams:TicketPageUrl"];
        var card = BuildTicketCard(ticket, pageUrl);
        return await SendAsync(workflowUri, card, "Ticket", ticket.TicketCode);
    }

    private static object BuildChangeRequestCard(ChangeRequest changeRequest, string? pageUrl)
    {
        var requester = FormatPerson(changeRequest.RequestorName, changeRequest.RequestorCode);
        var creator = FormatPerson(changeRequest.CreatedByName, changeRequest.CreatedByCode);
        var facts = new List<object>
        {
            new { title = "Mã yêu cầu", value = changeRequest.Code },
            new { title = "Người yêu cầu", value = requester },
            new { title = "Bộ phận", value = ValueOrDefault(changeRequest.RequestorDept) },
            new { title = "Người tạo", value = creator },
            new { title = "Mức ưu tiên", value = GetPriorityName(changeRequest.Priority) },
            new { title = "Trạng thái", value = ChangeRequestWorkflow.GetStatusName(changeRequest.Status) },
            new { title = "Thời gian tạo", value = changeRequest.CreatedAt.ToString("dd/MM/yyyy HH:mm") }
        };

        var body = new List<object>
        {
            new
            {
                type = "TextBlock",
                text = "Change Request mới",
                weight = "Bolder",
                size = "Medium",
                color = "Accent"
            },
            new
            {
                type = "TextBlock",
                text = changeRequest.Title,
                weight = "Bolder",
                wrap = true
            },
            new
            {
                type = "FactSet",
                facts
            }
        };

        var content = new Dictionary<string, object?>
        {
            ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
            ["type"] = "AdaptiveCard",
            ["version"] = "1.2",
            ["body"] = body
        };

        if (Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)
            && (pageUri.Scheme == Uri.UriSchemeHttps || pageUri.Scheme == Uri.UriSchemeHttp))
        {
            content["actions"] = new[]
            {
                new
                {
                    type = "Action.OpenUrl",
                    title = "Mở Change Request",
                    url = pageUri.ToString()
                }
            };
        }

        return new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    contentUrl = (string?)null,
                    content
                }
            }
        };
    }

    private static object BuildTicketCard(TicketLog ticket, string? pageUrl)
    {
        var facts = new List<object>
        {
            new { title = "Mã Ticket", value = ValueOrDefault(ticket.TicketCode) },
            new { title = "Loại Ticket", value = ValueOrDefault(ticket.TicketType) },
            new { title = "Người tạo", value = FormatPerson(ticket.UserName, ticket.UserCode) },
            new { title = "Bộ phận", value = ValueOrDefault(ticket.UserDepartment) },
            new { title = "Trạng thái", value = ticket.TicketStatus == 0 ? "Chờ xử lý" : "Đã tạo" },
            new { title = "Thời gian tạo", value = ticket.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "—" }
        };

        if (!string.IsNullOrWhiteSpace(ticket.TicketSubType))
        {
            facts.Insert(2, new
            {
                title = "Hạng mục hỗ trợ",
                value = TicketClassificationCatalog.GetSubTypeLabel(ticket.TicketSubType)
            });
        }

        var body = new List<object>
        {
            new
            {
                type = "TextBlock",
                text = "Ticket hỗ trợ mới",
                weight = "Bolder",
                size = "Medium",
                color = "Accent"
            },
            new
            {
                type = "TextBlock",
                text = ValueOrDefault(ticket.TicketTitle),
                weight = "Bolder",
                wrap = true
            },
            new
            {
                type = "FactSet",
                facts
            }
        };

        var content = new Dictionary<string, object?>
        {
            ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
            ["type"] = "AdaptiveCard",
            ["version"] = "1.2",
            ["body"] = body
        };

        if (Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)
            && (pageUri.Scheme == Uri.UriSchemeHttps || pageUri.Scheme == Uri.UriSchemeHttp))
        {
            content["actions"] = new[]
            {
                new
                {
                    type = "Action.OpenUrl",
                    title = "Mở danh sách Ticket",
                    url = pageUri.ToString()
                }
            };
        }

        return new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    contentUrl = (string?)null,
                    content
                }
            }
        };
    }

    private async Task<bool> SendAsync(Uri workflowUri, object card, string entityName, string? entityCode)
    {
        try
        {
            using var timeout = new CancellationTokenSource(RequestTimeout);
            using var response = await _httpClient.PostAsJsonAsync(workflowUri, card, timeout.Token);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Đã gửi thông báo Teams cho {EntityName} {EntityCode}.",
                    entityName,
                    entityCode);
                return true;
            }

            _logger.LogWarning(
                "Gửi thông báo Teams cho {EntityName} {EntityCode} thất bại với HTTP {StatusCode}.",
                entityName,
                entityCode,
                (int)response.StatusCode);
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Gửi thông báo Teams cho {EntityName} {EntityCode} quá thời gian {TimeoutSeconds} giây.",
                entityName,
                entityCode,
                RequestTimeout.TotalSeconds);
            return false;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Không thể kết nối Teams Workflow khi gửi {EntityName} {EntityCode}.",
                entityName,
                entityCode);
            return false;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Lỗi không xác định khi gửi thông báo Teams cho {EntityName} {EntityCode}.",
                entityName,
                entityCode);
            return false;
        }
    }

    private static string GetPriorityName(byte priority) => priority switch
    {
        1 => "Thấp",
        2 => "Trung bình",
        3 => "Cao",
        4 => "Khẩn cấp",
        _ => "Không xác định"
    };

    private static string FormatPerson(string? name, string? code)
    {
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(code))
            return $"{name} ({code})";
        return ValueOrDefault(name ?? code);
    }

    private static string ValueOrDefault(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;
}
