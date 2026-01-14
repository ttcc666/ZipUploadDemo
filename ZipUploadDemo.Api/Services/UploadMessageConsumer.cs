using DotNetCore.CAP;
using Microsoft.Extensions.Options;
using SqlSugar;
using ZipUploadDemo.Api.Models;
using ZipUploadDemo.Api.Options;

namespace ZipUploadDemo.Api.Services;

public class UploadMessageConsumer : ICapSubscribe
{
    private readonly UploadProcessingService _processingService;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<UploadMessageConsumer> _logger;
    private readonly StorageOptions _storageOptions;

    public UploadMessageConsumer(
        UploadProcessingService processingService,
        ISqlSugarClient db,
        IOptions<StorageOptions> storageOptions,
        ILogger<UploadMessageConsumer> logger)
    {
        _processingService = processingService;
        _db = db;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    [CapSubscribe("upload.process")]
    public async Task HandleUploadJob(BackgroundUploadJob jobMsg)
    {
        _logger.LogInformation("[CAP] 开始处理任务: {JobId}", jobMsg.JobId);

        // 1. 更新状态为 Processing
        await _db.Updateable<UploadJobEntity>()
                 .SetColumns(it => it.Status == UploadStatus.Processing)
                 .SetColumns(it => it.StartedAt == DateTime.UtcNow)
                 .SetColumns(it => it.Progress == 50)
                 .Where(it => it.JobId == jobMsg.JobId)
                 .ExecuteCommandAsync();

        try
        {
            // 检查文件是否存在
            var fileInfo = new FileInfo(jobMsg.ZipFilePath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"文件不存在: {jobMsg.ZipFilePath}");
            }

            // 直接处理已落盘的 ZIP 文件，避免构造 FormFile 和二次打开文件
            var result = await _processingService.ProcessSavedZipAsync(
                jobMsg.ZipFilePath,
                jobMsg.OriginalFileName,
                jobMsg.Workspace);

            // 2. 更新状态为 Completed
            await _db.Updateable<UploadJobEntity>()
                     .SetColumns(it => it.Status == UploadStatus.Completed)
                     .SetColumns(it => it.Progress == 100)
                     .SetColumns(it => it.CompletedAt == DateTime.UtcNow)
                     .SetColumns(it => it.BatchId == result.BatchId)
                     .Where(it => it.JobId == jobMsg.JobId)
                     .ExecuteCommandAsync();

            _logger.LogInformation("[CAP] 任务完成: {JobId}, BatchId: {BatchId}", jobMsg.JobId, result.BatchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CAP] 任务失败: {JobId}", jobMsg.JobId);

            // 3. 更新状态为 Failed
            await _db.Updateable<UploadJobEntity>()
                     .SetColumns(it => it.Status == UploadStatus.Failed)
                     .SetColumns(it => it.ErrorMessage == ex.Message)
                     .SetColumns(it => it.CompletedAt == DateTime.UtcNow)
                     .Where(it => it.JobId == jobMsg.JobId)
                     .ExecuteCommandAsync();

            if (_storageOptions.RetryOnFailure)
            {
                _logger.LogWarning("[CAP] 任务失败，将触发重试并保留工作目录: {JobId}", jobMsg.JobId);
                throw;
            }

            if (_storageOptions.CleanupWorkspaceOnFailure)
            {
                CleanupWorkspace(jobMsg.Workspace);
            }
            else
            {
                _logger.LogInformation("[CAP] 已保留失败工作目录: {Workspace}", jobMsg.Workspace);
            }
        }
    }

    private void CleanupWorkspace(string workspace)
    {
        try
        {
            if (Directory.Exists(workspace))
            {
                Directory.Delete(workspace, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理工作目录失败: {Workspace}", workspace);
        }
    }
}
