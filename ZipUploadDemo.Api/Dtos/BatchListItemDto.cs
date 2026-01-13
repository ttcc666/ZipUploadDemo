using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Dtos;

public class BatchListItemDto
{
    public long Id { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public string? ExcelFileName { get; set; }
    public int TotalRows { get; set; }
    public int TotalPdfs { get; set; }
    public UploadStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
