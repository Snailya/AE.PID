using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using AE.PID.Server;
using AE.PID.Server.Data;
using AE.PID.Server.PDMS.Extensions;
using AE.PID.Server.Services;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

// initialize the environment

// 创建数据库存储路径
if (!Directory.Exists(Constants.DatabasePath)) Directory.CreateDirectory(Constants.DatabasePath);

// 创建临时文件存储路径
if (!Directory.Exists(Constants.TmpPath)) Directory.CreateDirectory(Constants.TmpPath);

// 创建安装包存储路径
if (!Directory.Exists(Constants.InstallerPath)) Directory.CreateDirectory(Constants.InstallerPath);

// 创建StencilSnapshot存储路径
if (!Directory.Exists(Constants.StencilPath)) Directory.CreateDirectory(Constants.StencilPath);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IDocumentService, DocumentService>();
builder.Services.AddTransient<IRecommendService, RecommendService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler
                .IgnoreCycles; // to involve cycle reference, see https://learn.microsoft.com/zh-cn/ef/core/querying/related-data/serialization
    });

// enable api versioning 
builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.ReportApiVersions = true;
        options.ApiVersionReader = new HeaderApiVersionReader();
        options.AssumeDefaultVersionWhenUnspecified = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V";
            options.SubstituteApiVersionInUrl = true;
        }
    );

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen(options =>
{
    // Define multiple Swagger documents for different API versions
    var apiVersionDescriptions = new[] { "v3" }; // Replace with your actual API versions
    foreach (var version in apiVersionDescriptions)
        options.SwaggerDoc(version, new OpenApiInfo
        {
            Version = version,
            Title = $"API {version}",
            Description = $"API Documentation for version {version}",
            Contact = new OpenApiContact
            {
                Name = "Li Jingya",
                Email = "lijingya@chinaaie.com.cn"
            }
        });
});

// register db context
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=./PID_server.db;"));
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    builder.Services.AddDbContext<AppDbContext>(
        options => options.UseSqlite($"Data Source={Constants.DatabasePath}/PID_server.db;"));

builder.Services.AddHttpContextAccessor();

// use lowercase for route
builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

// register cors 
builder.Services.AddCors(options =>
{
    // options.AddPolicy("AllowAll",
    //     policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().AllowCredentials());
    options.AddPolicy("AllowIntranetIPRange",
        policy => policy.WithOrigins("http://172.18.0.0/22").AllowAnyMethod());
});

builder.Services.AddPDMS();

builder.Services.AddSignalR();

var app = builder.Build();

// initialize the database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var apiVersionDescriptions = new[] { "v3" };
    foreach (var version in apiVersionDescriptions)
        options.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"API {version}");
    options.RoutePrefix = string.Empty;
});

// set up the cors to allow only the intranet user
app.UseCors("AllowIntranetIPRange");

app.UseAuthorization();

app.MapControllers();

app.Run();