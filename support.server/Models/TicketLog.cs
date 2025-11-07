using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using support.server.Models;

namespace support.server.Models;

public partial class TicketLog
{
    public int TicketId { get; set; }

    public string? TicketCode { get; set; } = null;

    public string? TicketTitle { get; set; }

    public string? TicketType { get; set; }

    public string? TicketContent { get; set; }

    public byte? TicketStatus { get; set; }

    public string? FileAttachments { get; set; }
    [NotMapped]
    public IFormFile? UploadedFile { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    public string? UserCode { get; set; }

    public string? UserName { get; set; }

    public string? UserDepartment { get; set; }

    public string? UserContact { get; set; }

    public string? UserAssigneeCode { get; set; }

    public string? UserAssigneeName { get; set; }

    public string? UserAssigneeDepartment { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Note { get; set; }
}
