namespace ZipUploadDemo.Api.Options;

public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DbType { get; set; } = "SqlServer";
}
