namespace ZipUploadDemo.Api.Models;

public enum UploadStatus
{
    Queued = 0,       // 已加入队列
    Processing = 1,   // 处理中
    Uploaded = 2,     // 上传完成
    Parsed = 3,       // 解析完成
    Completed = 4,    // 全部完成
    Failed = 5        // 失败
}

public enum ParseStatus
{
    Unparsed = 0,
    Parsed = 1,
    MissingPdf = 2,
    InvalidRow = 3
}

public enum RowType
{
    Data = 0,         // 数据行
    Blank = 1,        // 空白行
    Header = 2        // 标题行
}

public enum DownloadStatus
{
    Queued = 0,        // 已加入队列
    Compressing = 1,   // 压缩中
    Ready = 2,         // 准备就绪（可下载）
    Downloaded = 3,    // 已下载
    Expired = 4,       // 已过期（文件已清理）
    Failed = 5         // 失败
}
