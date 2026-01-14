namespace ZipUploadDemo.Api.Services;

public class DownloadCompressJob
{
    public required string JobId { get; set; }
    public required long BatchId { get; set; }
    public required string BatchNo { get; set; }
}
