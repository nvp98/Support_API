using System;
using System.Collections.Generic;

namespace support.server.Models;

public partial class UserPermission
{
    public int PermissionId { get; set; }

    public string UserCode { get; set; } = null!;

    public string PermissionCode { get; set; } = null!;

    public bool? IsActive { get; set; }
}
