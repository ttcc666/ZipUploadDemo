using System.IO.Compression;
using Microsoft.Extensions.Options;
using SqlSugar;
using ZipUploadDemo.Api.Models;
using ZipUploadDemo.Api.Options;

namespace ZipUploadDemo.Api.Services;

public class DownloadCompressionService
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<DownloadCompressionService> _logger;
    private readonly StorageOptions _storageOptions;

    public DownloadCompressionService(
        ISqlSugarClient db,
        IOptions<StorageOptions> storageOptions,
        ILogger<DownloadCompressionService> logger)
    {
        _db = db;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// 压缩批次文件
    /// </summary>
    public async Task<CompressResult> CompressBatchFilesAsync(long batchId, string jobId)
    {
        // 1. 查询批次信息
        var batch = await _db.Queryable<UploadBatch>().FirstAsync(b => b.Id == batchId);
        if (batch == null)
        {
            throw new InvalidOperationException($"批次不存在: {batchId}");
        }

        // 2. 查询所有条目（包含 PDF 路径）
        var entries = await _db.Queryable<UploadEntry>()
            .Where(e => e.BatchId == batchId && e.RowType == RowType.Data)
            .ToListAsync();

        // 3. 准备输出目录
        var outputDir = PrepareDownloadWorkspace();
        var zipFileName = $"{batch.BatchNo}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
        var zipOutputPath = Path.Combine(outputDir, zipFileName);

        // 4. 收集文件路径
        var filesToCompress = new List<FileToCompress>();
        var missingFiles = new List<string>();

        // 4.1 添加 Excel 文件
        if (!string.IsNullOrWhiteSpace(batch.ExcelPath) && File.Exists(batch.ExcelPath))
        {
            filesToCompress.Add(new FileToCompress
            {
                PhysicalPath = batch.ExcelPath,
                ArchivePath = Path.GetFileName(batch.ExcelPath)
            });
        }
        else
        {
            missingFiles.Add($"Excel: {batch.ExcelFileName ?? "未知"}");
        }

        // 4.2 添加 PDF 文件
        foreach (var entry in entries)
        {
            if (entry.ParseStatus == ParseStatus.Parsed &&
                !string.IsNullOrWhiteSpace(entry.PdfPath))
            {
                if (File.Exists(entry.PdfPath))
                {
                    filesToCompress.Add(new FileToCompress
                    {
                        PhysicalPath = entry.PdfPath,
                        ArchivePath = $"pdfs/{Path.GetFileName(entry.PdfPath)}"
                    });
                }
                else
                {
                    missingFiles.Add($"PDF: {entry.PdfFileName ?? "未知"} (行 {entry.ExcelRowIndex})");
                }
            }
        }

        // 5. 创建 ZIP 文件
        using (var zipArchive = ZipFile.Open(zipOutputPath, ZipArchiveMode.Create))
        {
            int processedCount = 0;
            foreach (var file in filesToCompress)
            {
                try
                {
                    zipArchive.CreateEntryFromFile(file.PhysicalPath, file.ArchivePath, CompressionLevel.Optimal);
                    processedCount++;

                    // 更新进度（10-90%）
                    var progress = 10 + (int)((processedCount / (double)filesToCompress.Count) * 80);
                    await UpdateJobProgress(jobId, progress);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "添加文件到 ZIP 失败: {Path}", file.PhysicalPath);
                    missingFiles.Add($"压缩失败: {Path.GetFileName(file.PhysicalPath)}");
                }
            }

            // 6. 如果有缺失文件，生成说明文件
            if (missingFiles.Count > 0)
            {
                var missingFileContent = string.Join(Environment.NewLine, new[]
                {
                    "=== 缺失文件列表 ===",
                    $"批次号: {batch.BatchNo}",
                    $"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"缺失文件数: {missingFiles.Count}",
                    "",
                    "详细列表:",
                    string.Join(Environment.NewLine, missingFiles.Select((f, i) => $"{i + 1}. {f}"))
                });

                var missingFileEntry = zipArchive.CreateEntry("missing_files.txt");
                using var writer = new StreamWriter(missingFileEntry.Open());
                await writer.WriteAsync(missingFileContent);
            }
        }

        // 7. 获取文件大小
        var fileInfo = new FileInfo(zipOutputPath);

        return new CompressResult
        {
            Success = true,
            ZipOutputPath = zipOutputPath,
            ZipFileName = zipFileName,
            ZipFileSizeBytes = fileInfo.Length,
            TotalFiles = filesToCompress.Count,
            MissingFilesCount = missingFiles.Count
        };
    }

    private string PrepareDownloadWorkspace()
    {
        var root = _storageOptions.RootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(Path.GetTempPath(), "zip-upload-demo");
        }
        var downloadDir = Path.Combine(Path.GetFullPath(root), "downloads");
        Directory.CreateDirectory(downloadDir);
        return downloadDir;
    }

    private async Task UpdateJobProgress(string jobId, int progress)
    {
        await _db.Updateable<DownloadJobEntity>()
            .SetColumns(it => it.Progress == progress)
            .Where(it => it.JobId == jobId)
            .ExecuteCommandAsync();
    }

    private class FileToCompress
    {
        public string PhysicalPath { get; set; } = string.Empty;
        public string ArchivePath { get; set; } = string.Empty;
    }
}

public class CompressResult
{
    public bool Success { get; set; }
    public string ZipOutputPath { get; set; } = string.Empty;
    public string ZipFileName { get; set; } = string.Empty;
    public long ZipFileSizeBytes { get; set; }
    public int TotalFiles { get; set; }
    public int MissingFilesCount { get; set; }
}
