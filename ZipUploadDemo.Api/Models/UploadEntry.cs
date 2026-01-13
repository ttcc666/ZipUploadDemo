using SqlSugar;

namespace ZipUploadDemo.Api.Models;

[SugarTable("UploadEntry")]
public class UploadEntry
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(IsNullable = false)]
    public long BatchId { get; set; }

    [SugarColumn(IsNullable = false, ColumnName = "ExcelRowIndex")]
    public int ExcelRowIndex { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
    public string? RawText { get; set; }

    public RowType RowType { get; set; } = RowType.Blank;

    [SugarColumn(IsNullable = true)]
    public int? SeqNo { get; set; }

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? ProductName { get; set; }

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Model { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Quantity { get; set; }

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? SerialNo { get; set; }

    [SugarColumn(Length = 256, IsNullable = true)]
    public string? PdfFileName { get; set; }

    [SugarColumn(Length = 512, IsNullable = true)]
    public string? PdfPath { get; set; }

    public ParseStatus ParseStatus { get; set; } = ParseStatus.Unparsed;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? ErrorMessage { get; set; }
}
