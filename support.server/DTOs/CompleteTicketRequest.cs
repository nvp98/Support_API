using System.ComponentModel.DataAnnotations;

namespace support.server.DTOs;

public sealed class CompleteTicketRequest
{
    [StringLength(100_000, ErrorMessage = "Ghi chú hoàn thành không được vượt quá 100.000 ký tự.")]
    public string? CompletedNote { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Thời gian xử lý phải từ 1 phút.")]
    public int ProcessingMinutes { get; set; }

    [RegularExpression("^(OLD|NEW)$", ErrorMessage = "Phân loại lỗi không hợp lệ.")]
    public string? ErrorClassification { get; set; }

    [Required]
    [RegularExpression("^(IT|NT)$", ErrorMessage = "Phân loại xử lý không hợp lệ.")]
    public string HandlerClassification { get; set; } = string.Empty;
}
