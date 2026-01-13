using DotNetCore.CAP;
using Microsoft.AspNetCore.Http;
using SqlSugar;
using ZipUploadDemo.Api.Dtos;
using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Services;

public class UploadMessageConsumer : ICapSubscribe
{
    private readonly UploadProcessingService _processingService;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<UploadMessageConsumer> _logger;

    public UploadMessageConsumer(
        UploadProcessingService processingService,
        ISqlSugarClient db,
        ILogger<UploadMessageConsumer> logger)
    {
        _processingService = processingService;
        _db = db;
        _logger = logger;
    }

    [CapSubscribe("upload.process")]
    public async Task HandleUploadJob(BackgroundUploadJob jobMsg)
    {
        _logger.LogInformation($"[CAP] 开始处理任务: {jobMsg.JobId}");

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

            // 构造 IFormFile (模拟)
            UploadResultDto result;
            {
                await using var fs = new FileStream(jobMsg.ZipFilePath, FileMode.Open, FileAccess.Read);
                var formFile = new FormFile(fs, 0, fs.Length, "file", jobMsg.OriginalFileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/zip"
                };

                // 调用核心处理逻辑
                result = await _processingService.ProcessAsync(formFile);
            }

            // 2. 更新状态为 Completed
            await _db.Updateable<UploadJobEntity>()
                     .SetColumns(it => it.Status == UploadStatus.Completed)
                     .SetColumns(it => it.Progress == 100)
                     .SetColumns(it => it.CompletedAt == DateTime.UtcNow)
                     .SetColumns(it => it.BatchId == result.BatchId)
                     .Where(it => it.JobId == jobMsg.JobId)
                     .ExecuteCommandAsync();

            _logger.LogInformation($"[CAP] 任务完成: {jobMsg.JobId}, BatchId: {result.BatchId}");

            // 清理临时文件
            CleanupWorkspace(jobMsg.Workspace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CAP] 任务失败: {jobMsg.JobId}");

            // 3. 更新状态为 Failed
            await _db.Updateable<UploadJobEntity>()
                     .SetColumns(it => it.Status == UploadStatus.Failed)
                     .SetColumns(it => it.ErrorMessage == ex.Message)
                     .SetColumns(it => it.CompletedAt == DateTime.UtcNow)
                     .Where(it => it.JobId == jobMsg.JobId)
                     .ExecuteCommandAsync();
            
            // 抛出异常让 CAP 进行重试 (可选)
            // throw; 
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
