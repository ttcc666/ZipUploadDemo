using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Dtos;

public class UploadJobDto
{
    public string JobId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public UploadStatus Status { get; set; }
    public int Progress { get; set; } // 0-100
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public long? BatchId { get; set; }

    /// <summary>
    /// 处理耗时（秒）
    /// </summary>
    public double? ProcessingTimeSeconds
    {
        get
        {
            if (StartedAt.HasValue && CompletedAt.HasValue)
            {
                return (CompletedAt.Value - StartedAt.Value).TotalSeconds;
            }
            if (StartedAt.HasValue)
            {
                return (DateTime.UtcNow - StartedAt.Value).TotalSeconds;
            }
            return null;
        }
    }
}

public class AsyncUploadResponseDto
{
    public string JobId { get; set; } = string.Empty;
    public string Message { get; set; } = "文件已加入后台处理队列";
    public string StatusQueryUrl { get; set; } = string.Empty;
}
