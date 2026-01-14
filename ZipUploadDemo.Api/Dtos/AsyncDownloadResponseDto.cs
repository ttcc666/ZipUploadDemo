namespace ZipUploadDemo.Api.Dtos;

public class AsyncDownloadResponseDto
{
    public string JobId { get; set; } = string.Empty;
    public string Message { get; set; } = "文件压缩任务已加入队列";
    public string StatusQueryUrl { get; set; } = string.Empty;
}
