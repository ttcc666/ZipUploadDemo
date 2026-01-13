using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Services;

/// <summary>
/// 后台上传队列项
/// </summary>
public class BackgroundUploadJob
{
    public required string JobId { get; set; }
    public required string Workspace { get; set; }
    public required string ZipFilePath { get; set; }
    public required string OriginalFileName { get; set; }
    public UploadStatus Status { get; set; } = UploadStatus.Queued;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int Progress { get; set; } = 0; // 0-100
    public long? BatchId { get; set; }
}
