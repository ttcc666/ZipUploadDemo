using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using ZipUploadDemo.Api.Dtos;
using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BackgroundUploadController : ControllerBase
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<BackgroundUploadController> _logger;

    public BackgroundUploadController(
        ISqlSugarClient db,
        ILogger<BackgroundUploadController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 获取任务状态
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<UploadJobDto>> GetJobStatus(string jobId)
    {
        var job = await _db.Queryable<UploadJobEntity>().FirstAsync(j => j.JobId == jobId);
        if (job == null)
        {
            return NotFound(new { message = $"任务不存在: {jobId}" });
        }

        return Ok(new UploadJobDto
        {
            JobId = job.JobId,
            OriginalFileName = job.OriginalFileName,
            Status = job.Status,
            Progress = (int)job.Progress,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            ErrorMessage = job.ErrorMessage,
            BatchId = job.BatchId
        });
    }

    /// <summary>
    /// 获取所有任务列表
    /// </summary>
    [HttpGet("jobs")]
    public async Task<ActionResult<List<UploadJobDto>>> GetAllJobs()
    {
        var jobs = await _db.Queryable<UploadJobEntity>()
            .OrderByDescending(j => j.CreatedAt)
            .Take(100) // 限制返回最近100条
            .ToListAsync();

        return Ok(jobs.Select(j => new UploadJobDto
        {
            JobId = j.JobId,
            OriginalFileName = j.OriginalFileName,
            Status = j.Status,
            Progress = (int)j.Progress,
            CreatedAt = j.CreatedAt,
            StartedAt = j.StartedAt,
            CompletedAt = j.CompletedAt,
            ErrorMessage = j.ErrorMessage,
            BatchId = j.BatchId
        }).ToList());
    }

    /// <summary>
    /// 获取队列统计信息
    /// </summary>
    [HttpGet("jobs/stats")]
    public async Task<ActionResult<object>> GetQueueStats()
    {
        // 简单统计
        var allJobs = await _db.Queryable<UploadJobEntity>().ToListAsync();
        
        var stats = new
        {
            Total = allJobs.Count,
            Queued = allJobs.Count(j => j.Status == UploadStatus.Queued),
            Processing = allJobs.Count(j => j.Status == UploadStatus.Processing),
            Completed = allJobs.Count(j => j.Status == UploadStatus.Completed),
            Failed = allJobs.Count(j => j.Status == UploadStatus.Failed),
            AverageProcessingTimeSeconds = allJobs
                .Where(j => j.StartedAt.HasValue && j.CompletedAt.HasValue)
                .Select(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalSeconds)
                .DefaultIfEmpty(0)
                .Average()
        };

        return Ok(stats);
    }
}
