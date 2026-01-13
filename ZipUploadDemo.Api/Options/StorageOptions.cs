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

    /// <summary>
    /// 后台任务失败后是否清理工作目录，默认清理以避免磁盘堆积
    /// </summary>
    public bool CleanupWorkspaceOnFailure { get; set; } = true;

    /// <summary>
    /// 后台任务失败时是否抛出异常触发 CAP 重试，默认不重试
    /// </summary>
    public bool RetryOnFailure { get; set; } = false;
}
