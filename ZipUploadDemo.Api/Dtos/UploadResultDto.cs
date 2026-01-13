using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Dtos;

public class UploadResultDto
{
    public long BatchId { get; set; }
    public string? BatchNo { get; set; }
    public int TotalRows { get; set; }
    public int ParsedRows { get; set; }
    public int MissingPdfRows { get; set; }
    public int InvalidRows { get; set; }
    public List<RowErrorDto> Errors { get; set; } = new();
}

public class RowErrorDto
{
    public int RowIndex { get; set; }
    public string? RawText { get; set; }
    public ParseStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}
