using SqlSugar;

namespace ZipUploadDemo.Api.Models;

[SugarTable("UploadBatch")]
public class UploadBatch
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string BatchNo { get; set; } = string.Empty;

    [SugarColumn(Length = 256, IsNullable = true)]
    public string? ExcelFileName { get; set; }

    [SugarColumn(Length = 512, IsNullable = true)]
    public string? ExcelPath { get; set; }

    public int TotalRows { get; set; }

    public int TotalPdfs { get; set; }

    public UploadStatus Status { get; set; } = UploadStatus.Uploaded;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? UpdatedBy { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }
}
