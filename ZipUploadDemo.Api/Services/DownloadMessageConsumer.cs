using DotNetCore.CAP;
using SqlSugar;
using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Services;

public class DownloadMessageConsumer : ICapSubscribe
{
    private readonly DownloadCompressionService _compressionService;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<DownloadMessageConsumer> _logger;

    public DownloadMessageConsumer(
        DownloadCompressionService compressionService,
        ISqlSugarClient db,
        ILogger<DownloadMessageConsumer> logger)
    {
        _compressionService = compressionService;
        _db = db;
        _logger = logger;
    }

    [CapSubscribe("download.compress")]
    public async Task HandleDownloadCompressJob(DownloadCompressJob jobMsg)
    {
        _logger.LogInformation("[CAP] 开始处理下载压缩任务: {JobId}, BatchId: {BatchId}",
            jobMsg.JobId, jobMsg.BatchId);

        // 1. 更新状态为 Compressing
        await _db.Updateable<DownloadJobEntity>()
            .SetColumns(it => it.Status == DownloadStatus.Compressing)
            .SetColumns(it => it.StartedAt == DateTime.UtcNow)
            .SetColumns(it => it.Progress == 10)
            .Where(it => it.JobId == jobMsg.JobId)
            .ExecuteCommandAsync();

        try
        {
            // 2. 执行压缩
            var result = await _compressionService.CompressBatchFilesAsync(jobMsg.BatchId, jobMsg.JobId);

            // 3. 更新状态为 Ready
            await _db.Updateable<DownloadJobEntity>()
                .SetColumns(it => it.Status == DownloadStatus.Ready)
                .SetColumns(it => it.Progress == 100)
                .SetColumns(it => it.CompletedAt == DateTime.UtcNow)
                .SetColumns(it => it.ZipOutputPath == result.ZipOutputPath)
                .SetColumns(it => it.ZipFileName == result.ZipFileName)
                .SetColumns(it => it.ZipFileSizeBytes == result.ZipFileSizeBytes)
                .SetColumns(it => it.TotalFiles == result.TotalFiles)
                .SetColumns(it => it.MissingFilesCount == result.MissingFilesCount)
                .Where(it => it.JobId == jobMsg.JobId)
                .ExecuteCommandAsync();

            _logger.LogInformation("[CAP] 下载压缩任务完成: {JobId}, 文件: {FileName}, 大小: {Size} bytes",
                jobMsg.JobId, result.ZipFileName, result.ZipFileSizeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CAP] 下载压缩任务失败: {JobId}", jobMsg.JobId);

            // 4. 更新状态为 Failed
            await _db.Updateable<DownloadJobEntity>()
                .SetColumns(it => it.Status == DownloadStatus.Failed)
                .SetColumns(it => it.ErrorMessage == ex.Message)
                .SetColumns(it => it.CompletedAt == DateTime.UtcNow)
                .Where(it => it.JobId == jobMsg.JobId)
                .ExecuteCommandAsync();

            throw; // 触发 CAP 重试机制
        }
    }
}
