# 接口文档

## 基础信息
- 基址：`http://localhost:5016`（按 `launchSettings` 默认；部署请自调）
- 认证：无（请按需加鉴权）
- 返回格式：`application/json`
- CORS：默认允许任意源/头/方法
- 上传限制：Kestrel 设置 200MB MaxRequestBodySize

## 上传并解析
**POST** `/api/upload`

- 功能：上传 ZIP（包含 Excel 与对应 PDF），解析行信息并入库。
- 请求：`multipart/form-data`，字段 `file`（ZIP 文件）。
- 成功响应 200 `UploadResultDto`：
  - `BatchId`(long) 批次ID
  - `BatchNo`(string) 证书/前缀
  - `TotalRows`(int) Excel 总行数（含空行）
  - `ParsedRows`(int) 成功解析的明细行数
  - `MissingPdfRows`(int) 缺少 PDF 的行数
  - `InvalidRows`(int) 解析失败的行数
  - `Errors`(array) 行级错误：`RowIndex`、`RawText`、`Status`(`Parsed`/`MissingPdf`/`InvalidRow`)、`ErrorMessage`
- 示例：
```bash
curl -F "file=@25581218-12-CF.zip" http://localhost:5016/api/upload
```

## 查询上传履历
**GET** `/api/upload/batches?batchNo={keyword}&page={n}&pageSize={m}`

- 功能：分页查询上传批次（履历）。
- 参数：
  - `batchNo` 可选，模糊匹配批次号/证书号前缀
  - `page` 页码，默认 1
  - `pageSize` 每页条数，默认 20，最大 200
- 成功响应 200 `PagedResult<BatchListItemDto>`：
  - `Total`(int) 总数
  - `Items`(array)：`Id`、`BatchNo`、`ExcelFileName`、`TotalRows`、`TotalPdfs`、`Status`、`CreatedAt`

## 查询批次行数据
**GET** `/api/upload/batches/{batchId}/entries?page={n}&pageSize={m}`

- 功能：分页查询指定批次的行数据（含原始文本和解析结果）。
- 参数：
  - `batchId` 必选，上传批次 ID
  - `page` 页码，默认 1
  - `pageSize` 每页条数，默认 100，最大 500
- 成功响应 200 `PagedResult<EntryDto>`：
  - `Total`(int) 总数
  - `Items`(array)：
    - `RowIndex` 行号（Excel 1-based）
    - `RowType` Data/Header/Footer/Blank
    - `SeqNo`、`ProductName`、`Model`、`Quantity`、`SerialNo`
    - `PdfFileName`
    - `ParseStatus` Parsed/MissingPdf/InvalidRow
    - `ErrorMessage`
    - `RawText` 原单元格全文

## 统一模型
- `PagedResult<T>`：`Total` + `Items` 数组
- `UploadResultDto`：上传解析结果（见上）

## 错误说明
- 4xx：参数或文件问题（未找到 Excel、缺少前缀或未匹配到 PDF 等），`message` 字段描述。
- 5xx：服务器内部错误，需查看日志。

## 配置提示
- 数据库连接：`appsettings*.json` 中 `Database:ConnectionString`（SQL Server）。
- 存储目录：`Storage:RootPath`（解压及临时文件）。
- Swagger：开发环境默认启用，可在浏览器访问 `/swagger` 进行调试。
