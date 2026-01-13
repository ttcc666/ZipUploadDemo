namespace ZipUploadDemo.Api.Options;

public class StorageOptions
{
    public string RootPath { get; set; } = "storage";

    /// <summary>
    /// 最大并发上传处理数量，默认为 2
    /// </summary>
    public int MaxConcurrentUploads { get; set; } = 2;

    /// <summary>
    /// 是否启用后台处理队列，默认为 true
    /// </summary>
    public bool EnableBackgroundProcessing { get; set; } = true;

    /// <summary>
    /// 小文件阈值（MB），小于此值则同步处理，大于此值则异步处理
    /// 默认 10MB
    /// </summary>
    public int AsyncFileSizeThresholdMB { get; set; } = 10;
}
