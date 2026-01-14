using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Dtos;

public class DownloadJobDto
{
    public string JobId { get; set; } = string.Empty;
    public long BatchId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public DownloadStatus Status { get; set; }
    public int Progress { get; set; } // 0-100
    public string? ZipFileName { get; set; }
    public long ZipFileSizeBytes { get; set; }
    public int TotalFiles { get; set; }
    public int MissingFilesCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DownloadUrl { get; set; } // 仅当 Status == Ready 时有值
}
