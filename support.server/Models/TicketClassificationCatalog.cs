namespace support.server.Models;

public static class TicketClassificationCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> SubTypesByType =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["SOFT"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "EOFFICE",
                "MS365",
                "BK_SOFTWARE",
                "ACCESS_CONTROL",
                "WINDOWS_INSTALL",
                "OTHER_SOFTWARE"
            },
            ["HARD"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CAMERA",
                "PRINTER",
                "RAM_REPLACEMENT",
                "DRIVE_REPLACEMENT",
                "OTHER_HARDWARE"
            },
            ["SAP"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

    private static readonly IReadOnlyDictionary<string, string> SubTypeLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["EOFFICE"] = "EOffice",
            ["MS365"] = "MS 365 (teams, mail,...)",
            ["BK_SOFTWARE"] = "Phần mềm BK",
            ["ACCESS_CONTROL"] = "AccessControl",
            ["WINDOWS_INSTALL"] = "Cài Win",
            ["OTHER_SOFTWARE"] = "Phần mềm khác",
            ["CAMERA"] = "Camera",
            ["PRINTER"] = "Máy in",
            ["RAM_REPLACEMENT"] = "Thay RAM",
            ["DRIVE_REPLACEMENT"] = "Thay Ổ cứng",
            ["OTHER_HARDWARE"] = "Phần cứng khác"
        };

    public static string? Validate(string? ticketType, string? ticketSubType)
    {
        if (ticketType is null || !SubTypesByType.TryGetValue(ticketType, out var configuredSubTypes))
            return "Nhóm yêu cầu không hợp lệ.";

        if (configuredSubTypes.Count == 0)
            return ticketSubType is null
                ? null
                : "Nhóm yêu cầu này chưa có hạng mục hỗ trợ được cấu hình.";

        if (ticketSubType is null)
            return "Vui lòng chọn hạng mục hỗ trợ.";

        return configuredSubTypes.Contains(ticketSubType)
            ? null
            : "Hạng mục hỗ trợ không thuộc nhóm yêu cầu đã chọn.";
    }

    public static string GetSubTypeLabel(string? ticketSubType)
    {
        if (string.IsNullOrWhiteSpace(ticketSubType))
            return string.Empty;

        return SubTypeLabels.TryGetValue(ticketSubType, out var label)
            ? label
            : ticketSubType;
    }
}
