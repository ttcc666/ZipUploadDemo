using SqlSugar;
using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Models;

[SugarTable("UploadJobs")]
public class UploadJobEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public string JobId { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;
    public string ZipFilePath { get; set; } = string.Empty;
    public string Workspace { get; set; } = string.Empty;

    public UploadStatus Status { get; set; }
    public double Progress { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public string? ErrorMessage { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public long? BatchId { get; set; }

    public DateTime CreatedAt { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public DateTime? StartedAt { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public DateTime? CompletedAt { get; set; }
}
