using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using ZipUploadDemo.Api.Dtos;
using ZipUploadDemo.Api.Models;
using ZipUploadDemo.Api.Services;

namespace ZipUploadDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly ICapPublisher _capBus;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(
        ICapPublisher capBus,
        ISqlSugarClient db,
        ILogger<DownloadController> logger)
    {
        _capBus = capBus;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 创建下载任务（异步）
    /// </summary>
    [HttpPost("batches/{batchId:long}")]
    public async Task<ActionResult<AsyncDownloadResponseDto>> CreateDownloadJob(long batchId)
    {
        // 1. 验证批次是否存在
        var batch = await _db.Queryable<UploadBatch>().FirstAsync(b => b.Id == batchId);
        if (batch == null)
        {
            return NotFound(new { message = $"批次不存在: {batchId}" });
        }

        // 2. 检查是否已有进行中的下载任务
        var existingJob = await _db.Queryable<DownloadJobEntity>()
            .Where(j => j.BatchId == batchId &&
                       (j.Status == DownloadStatus.Queued ||
                        j.Status == DownloadStatus.Compressing ||
                        j.Status == DownloadStatus.Ready))
            .OrderByDescending(j => j.CreatedAt)
            .FirstAsync();

        if (existingJob != null)
        {
            // 如果已有可用任务，直接返回
            return Ok(new AsyncDownloadResponseDto
            {
                JobId = existingJob.JobId,
                Message = existingJob.Status == DownloadStatus.Ready
                    ? "文件已准备就绪，可直接下载"
                    : "下载任务正在处理中",
                StatusQueryUrl = $"/api/download/jobs/{existingJob.JobId}"
            });
        }

        // 3. 创建新的下载任务
        var jobId = Guid.NewGuid().ToString("N");
        var jobEntity = new DownloadJobEntity
        {
            JobId = jobId,
            BatchId = batchId,
            BatchNo = batch.BatchNo,
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24) // 24小时后过期
        };
        await _db.Insertable(jobEntity).ExecuteCommandAsync();

        // 4. 发布 CAP 消息
        await _capBus.PublishAsync("download.compress", new DownloadCompressJob
        {
            JobId = jobId,
            BatchId = batchId,
            BatchNo = batch.BatchNo
        });

        _logger.LogInformation("创建下载任务: JobId={JobId}, BatchId={BatchId}", jobId, batchId);

        return Ok(new AsyncDownloadResponseDto
        {
            JobId = jobId,
            Message = "文件压缩任务已加入队列",
            StatusQueryUrl = $"/api/download/jobs/{jobId}"
        });
    }

    /// <summary>
    /// 查询下载任务状态
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<DownloadJobDto>> GetJobStatus(string jobId)
    {
        var job = await _db.Queryable<DownloadJobEntity>().FirstAsync(j => j.JobId == jobId);
        if (job == null)
        {
            return NotFound(new { message = $"任务不存在: {jobId}" });
        }

        var dto = new DownloadJobDto
        {
            JobId = job.JobId,
            BatchId = job.BatchId,
            BatchNo = job.BatchNo,
            Status = job.Status,
            Progress = (int)job.Progress,
            ZipFileName = job.ZipFileName,
            ZipFileSizeBytes = job.ZipFileSizeBytes,
            TotalFiles = job.TotalFiles,
            MissingFilesCount = job.MissingFilesCount,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            ExpiresAt = job.ExpiresAt,
            ErrorMessage = job.ErrorMessage
        };

        // 如果状态为 Ready，添加下载链接
        if (job.Status == DownloadStatus.Ready)
        {
            dto.DownloadUrl = $"/api/download/jobs/{jobId}/file";
        }

        return Ok(dto);
    }

    /// <summary>
    /// 下载文件（物理文件流）
    /// </summary>
    [HttpGet("jobs/{jobId}/file")]
    public async Task<IActionResult> DownloadFile(string jobId)
    {
        var job = await _db.Queryable<DownloadJobEntity>().FirstAsync(j => j.JobId == jobId);
        if (job == null)
        {
            return NotFound(new { message = $"任务不存在: {jobId}" });
        }

        if (job.Status != DownloadStatus.Ready)
        {
            return BadRequest(new { message = $"文件未准备就绪，当前状态: {job.Status}" });
        }

        if (string.IsNullOrWhiteSpace(job.ZipOutputPath) || !System.IO.File.Exists(job.ZipOutputPath))
        {
            return NotFound(new { message = "文件不存在或已被清理" });
        }

        // 检查是否过期
        if (job.ExpiresAt.HasValue && DateTime.UtcNow > job.ExpiresAt.Value)
        {
            return BadRequest(new { message = "文件已过期" });
        }

        // 更新下载时间和状态
        await _db.Updateable<DownloadJobEntity>()
            .SetColumns(it => it.Status == DownloadStatus.Downloaded)
            .SetColumns(it => it.DownloadedAt == DateTime.UtcNow)
            .Where(it => it.JobId == jobId)
            .ExecuteCommandAsync();

        _logger.LogInformation("下载文件: JobId={JobId}, FileName={FileName}", jobId, job.ZipFileName);

        // 返回文件流
        var fileStream = new FileStream(job.ZipOutputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(fileStream, "application/zip", job.ZipFileName ?? $"{job.BatchNo}.zip");
    }

    /// <summary>
    /// 清理过期文件（可由定时任务调用）
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult<object>> CleanupExpiredFiles()
    {
        var expiredJobs = await _db.Queryable<DownloadJobEntity>()
            .Where(j => j.Status == DownloadStatus.Ready &&
                       j.ExpiresAt.HasValue &&
                       j.ExpiresAt.Value < DateTime.UtcNow)
            .ToListAsync();

        int cleanedCount = 0;
        foreach (var job in expiredJobs)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(job.ZipOutputPath) && System.IO.File.Exists(job.ZipOutputPath))
                {
                    System.IO.File.Delete(job.ZipOutputPath);
                    cleanedCount++;
                }

                await _db.Updateable<DownloadJobEntity>()
                    .SetColumns(it => it.Status == DownloadStatus.Expired)
                    .Where(it => it.JobId == job.JobId)
                    .ExecuteCommandAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理文件失败: {JobId}", job.JobId);
            }
        }

        _logger.LogInformation("清理过期文件完成: 清理数量={Count}", cleanedCount);

        return Ok(new { message = $"已清理 {cleanedCount} 个过期文件" });
    }
}
