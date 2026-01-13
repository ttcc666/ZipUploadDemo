using Microsoft.Extensions.Options;
using Savorboard.CAP.InMemoryMessageQueue;
using SqlSugar;
using ZipUploadDemo.Api.Models;
using ZipUploadDemo.Api.Options;
using ZipUploadDemo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 配置 Kestrel，放宽上传大小限制
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200 * 1024 * 1024; // 200 MB
});

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddSingleton<ISqlSugarClient>(sp =>
{
    var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    if (string.IsNullOrWhiteSpace(dbOptions.ConnectionString))
    {
        throw new InvalidOperationException("缺少数据库连接字符串配置");
    }

    var dbType = SqlSugar.DbType.SqlServer;
    if (!Enum.TryParse(dbOptions.DbType, ignoreCase: true, out dbType))
    {
        dbType = SqlSugar.DbType.SqlServer;
    }

    var client = new SqlSugarScope(new ConnectionConfig
    {
        ConnectionString = dbOptions.ConnectionString,
        DbType = dbType,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute
    });

    client.Aop.OnLogExecuting = (sql, pars) =>
    {
        Console.WriteLine($"SQL: {sql}");
    };

    // Code First: 初始化数据库表
    client.CodeFirst.InitTables<UploadBatch, UploadEntry, UploadJobEntity>();
    return client;
});

// CAP 配置
builder.Services.AddCap(x =>
{
    var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>();
    if (dbOptions is null || string.IsNullOrWhiteSpace(dbOptions.ConnectionString))
    {
        throw new InvalidOperationException("缺少数据库连接字符串配置");
    }
    // 使用 SQL Server 存储事件日志 (Cap.Published, Cap.Received)
    x.UseSqlServer(dbOptions.ConnectionString);

    // 使用内存消息队列进行传输 (仅适用于单机/测试，生产环境建议 RabbitMQ)
    x.UseInMemoryMessageQueue();

    x.UseDashboard(); // 仪表板地址: /cap
});

builder.Services.AddScoped<UploadProcessingService>();
builder.Services.AddScoped<UploadQueryService>();
builder.Services.AddScoped<UploadMessageConsumer>(); // 注册消费者

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable serving static files
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
