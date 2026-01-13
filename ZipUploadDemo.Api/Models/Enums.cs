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

public enum RowType
{
    Data = 1,
    Header = 2,
    Footer = 3,
    Blank = 4
}

public enum ParseStatus
{
    Unparsed = 0,
    Parsed = 1,
    MissingPdf = 2,
    InvalidRow = 3
}
