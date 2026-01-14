using SqlSugar;

namespace ZipUploadDemo.Api.Models;

[SugarTable("DownloadJobs")]
public class DownloadJobEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public string JobId { get; set; } = string.Empty;

    [SugarColumn(IsNullable = false)]
    public long BatchId { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string BatchNo { get; set; } = string.Empty;

    [SugarColumn(Length = 512, IsNullable = true)]
    public string? ZipOutputPath { get; set; }

    [SugarColumn(Length = 256, IsNullable = true)]
    public string? ZipFileName { get; set; }

    public long ZipFileSizeBytes { get; set; }

    public int TotalFiles { get; set; }
    public int MissingFilesCount { get; set; }

    public DownloadStatus Status { get; set; }
    public double Progress { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? StartedAt { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? CompletedAt { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? DownloadedAt { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? ExpiresAt { get; set; }
}
