namespace support.server.Models.SLC;

public enum ChangeRequestStatus : byte
{
    Draft = 0,
    WaitingAcceptance = 1,
    WaitingApproval = 2,
    WaitingCompletion = 3,
    Completed = 4,
    Rejected = 5
}

public static class ChangeRequestWorkflow
{
    public const string AdminRole = "admin";
    public const string DeveloperRole = "developer";
    public const string ApproverRole = "approver";

    public static string GetStatusName(byte status) => (ChangeRequestStatus)status switch
    {
        ChangeRequestStatus.Draft => "Bản nháp",
        ChangeRequestStatus.WaitingAcceptance => "Chờ tiếp nhận",
        ChangeRequestStatus.WaitingApproval => "Chờ xác nhận",
        ChangeRequestStatus.WaitingCompletion => "Chờ hoàn thành",
        ChangeRequestStatus.Completed => "Hoàn thành",
        ChangeRequestStatus.Rejected => "Từ chối",
        _ => "Không xác định"
    };

    public static IReadOnlyList<string> GetAllowedActions(
        byte status,
        IReadOnlySet<string> roles,
        string actorCode,
        string? createdByCode)
    {
        var actions = new List<string>();
        var isAdmin = roles.Contains(AdminRole);
        var isCreator = !string.IsNullOrWhiteSpace(createdByCode)
            && string.Equals(actorCode, createdByCode, StringComparison.OrdinalIgnoreCase);

        if ((isAdmin || isCreator) && status <= (byte)ChangeRequestStatus.WaitingAcceptance)
        {
            actions.Add("EDIT");
            actions.Add("DELETE");
        }

        if (isAdmin)
        {
            if (status <= (byte)ChangeRequestStatus.WaitingCompletion)
                actions.Add("ADD_REVISION");

            if (status == (byte)ChangeRequestStatus.WaitingCompletion)
                actions.Add("COMPLETE");
        }

        if (roles.Contains(DeveloperRole) && status == (byte)ChangeRequestStatus.WaitingAcceptance)
            actions.Add("ACCEPT");

        if (roles.Contains(ApproverRole) && status == (byte)ChangeRequestStatus.WaitingApproval)
        {
            actions.Add("APPROVE");
            actions.Add("REJECT");
        }

        return actions;
    }
}
