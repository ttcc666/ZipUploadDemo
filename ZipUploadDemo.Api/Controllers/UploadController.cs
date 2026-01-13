using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;
using ZipUploadDemo.Api.Dtos;
using ZipUploadDemo.Api.Models;
using ZipUploadDemo.Api.Options;
using ZipUploadDemo.Api.Services;

namespace ZipUploadDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly UploadProcessingService _processing;
    private readonly UploadQueryService _query;
    private readonly ICapPublisher _capBus;
    private readonly ISqlSugarClient _db;
    private readonly StorageOptions _storageOptions;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        UploadProcessingService processing,
        UploadQueryService query,
        ICapPublisher capBus,
        ISqlSugarClient db,
        IOptions<StorageOptions> storageOptions,
        ILogger<UploadController> logger)
    {
        _processing = processing;
        _query = query;
        _capBus = capBus;
        _db = db;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// 上传 ZIP 文件
    /// 小文件（<阈值）同步处理，大文件（≥阈值）异步处理
    /// </summary>
    [HttpPost]
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)] // 200 MB 默认上限，可按需调整
    public async Task<ActionResult<object>> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            // 判断是否需要异步处理
            var fileSizeMB = file.Length / (1024 * 1024);
            var useAsync = _storageOptions.EnableBackgroundProcessing &&
                          fileSizeMB >= _storageOptions.AsyncFileSizeThresholdMB;

            if (useAsync)
            {
                _logger.LogInformation("大文件检测到，使用异步处理: {FileName} ({Size}MB)", file.FileName, fileSizeMB);

                // 保存文件到临时工作目录
                var workspace = PrepareWorkspace();
                var savedZip = Path.Combine(workspace, file.FileName);

                await using (var fs = new FileStream(savedZip, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920))
                {
                    await file.CopyToAsync(fs, cancellationToken);
                }

                var jobId = Guid.NewGuid().ToString("N");

                // 1. 在数据库创建任务记录
                var jobEntity = new UploadJobEntity
                {
                    JobId = jobId,
                    OriginalFileName = file.FileName,
                    ZipFilePath = savedZip,
                    Workspace = workspace,
                    Status = UploadStatus.Queued,
                    Progress = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await _db.Insertable(jobEntity).ExecuteCommandAsync();

                // 2. 发送 CAP 消息
                await _capBus.PublishAsync("upload.process", new BackgroundUploadJob
                {
                    JobId = jobId,
                    Workspace = workspace,
                    ZipFilePath = savedZip,
                    OriginalFileName = file.FileName,
                    Status = UploadStatus.Queued,
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new AsyncUploadResponseDto
                {
                    JobId = jobId,
                    Message = $"文件已加入后台处理队列（大小: {fileSizeMB:F2}MB）",
                    StatusQueryUrl = $"/api/backgroundupload/jobs/{jobId}"
                });
            }
            else
            {
                // 小文件同步处理
                _logger.LogInformation("小文件同步处理: {FileName} ({Size}MB)", file.FileName, fileSizeMB);
                var result = await _processing.ProcessAsync(file, cancellationToken);
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传处理失败");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 分页查询上传批次
    /// </summary>
    [HttpGet("batches")]
    public async Task<ActionResult<PagedResult<BatchListItemDto>>> GetBatches([FromQuery] string? batchNo, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _query.QueryBatchesAsync(batchNo, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// 分页查询批次行数据
    /// </summary>
    [HttpGet("batches/{batchId:long}/entries")]
    public async Task<ActionResult<PagedResult<EntryDto>>> GetEntries([FromRoute] long batchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var result = await _query.QueryEntriesAsync(batchId, page, pageSize);
        return Ok(result);
    }

    private string PrepareWorkspace()
    {
        var root = _storageOptions.RootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(Path.GetTempPath(), "zip-upload-demo");
        }
        var workspace = Path.Combine(Path.GetFullPath(root), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        return workspace;
    }
}
