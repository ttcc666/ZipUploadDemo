using SqlSugar;
using ZipUploadDemo.Api.Dtos;
using ZipUploadDemo.Api.Models;

namespace ZipUploadDemo.Api.Services;

public class UploadQueryService
{
    private readonly ISqlSugarClient _db;

    public UploadQueryService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<PagedResult<BatchListItemDto>> QueryBatchesAsync(string? batchNo, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.Queryable<UploadBatch>();
        if (!string.IsNullOrWhiteSpace(batchNo))
        {
            query = query.Where(b => b.BatchNo.Contains(batchNo));
        }

        RefAsync<int> total = 0;
        var data = await query.OrderBy(b => b.CreatedAt, OrderByType.Desc)
            .ToPageListAsync(page, pageSize, total);

        // 在内存中映射到 DTO，避免 SqlSugar 投影时的列名冲突
        var result = data.Select(b => new BatchListItemDto
        {
            Id = b.Id,
            BatchNo = b.BatchNo,
            ExcelFileName = b.ExcelFileName,
            TotalRows = b.TotalRows,
            TotalPdfs = b.TotalPdfs,
            Status = b.Status,
            CreatedAt = b.CreatedAt
        }).ToList();

        return new PagedResult<BatchListItemDto>
        {
            Total = total,
            Items = result
        };
    }

    public async Task<PagedResult<EntryDto>> QueryEntriesAsync(long batchId, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = _db.Queryable<UploadEntry>().Where(e => e.BatchId == batchId);

        RefAsync<int> total = 0;
        var data = await query.OrderBy(e => e.ExcelRowIndex).ToPageListAsync(page, pageSize, total);

        // 直接在数据库层面进行投影，减少内存分配和数据传输
        var result = data.Select(e => new EntryDto
        {
            Id = e.Id,
            RowIndex = e.ExcelRowIndex,
            RowType = e.RowType,
            SeqNo = e.SeqNo,
            ProductName = e.ProductName,
            Model = e.Model,
            Quantity = e.Quantity,
            SerialNo = e.SerialNo,
            PdfFileName = e.PdfFileName,
            ParseStatus = e.ParseStatus,
            ErrorMessage = e.ErrorMessage,
            RawText = e.RawText
        }).ToList();

        return new PagedResult<EntryDto>
        {
            Total = total,
            Items = result
        };
    }
}
