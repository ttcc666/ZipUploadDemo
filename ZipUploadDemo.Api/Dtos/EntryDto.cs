using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Dtos;

public class EntryDto
{
    public long Id { get; set; }
    public int RowIndex { get; set; }
    public RowType RowType { get; set; }
    public int? SeqNo { get; set; }
    public string? ProductName { get; set; }
    public string? Model { get; set; }
    public int? Quantity { get; set; }
    public string? SerialNo { get; set; }
    public string? PdfFileName { get; set; }
    public ParseStatus ParseStatus { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawText { get; set; }
}
