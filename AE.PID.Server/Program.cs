using System.Runtime.InteropServices;
using AE.PID.Server;
using AE.PID.Server.Apis;
using AE.PID.Server.Data;
using AE.PID.Server.PDMS.Extensions;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

// initialize the environment
EnsureEnvironmentPathsExist();

var builder = WebApplication.CreateBuilder(args);

// api versioning support
builder.Services
    .AddApiVersioning(options => { options.ApiVersionReader = new UrlSegmentApiVersionReader(); })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    });
;

// minimal api endpoints 识别
builder.Services.AddEndpointsApiExplorer();

// generate openapi document for each api version
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API V1", Version = "v1" });
    options.SwaggerDoc("v2", new OpenApiInfo { Title = "My API V2", Version = "v2" });
    options.SwaggerDoc("v3", new OpenApiInfo
    {
        Title = "AE PID API V3",
        Description = "AE PID 后端文档",
        Version = "v3",
        Contact = new OpenApiContact
        {
            Name = "Li Jingya",
            Email = "lijingya@chianaie.com.cn",
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { "Mobile", new OpenApiString("13001129011") },
                { "Tel.", new OpenApiString("022-27888633") },
                { "Dept.", new OpenApiString("涂装工程院/装备事业部/产品开发室") }
            }
        }
    });
});

// register service
builder.Services.AddTransient<IVisioDocumentService, VisioDocumentService>();
builder.Services.AddTransient<IRecommendService, RecommendService>();

// register db context
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=./PID_server.db;"));
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(
        $"Data Source={PathConstants.DatabasePath}/PID_server.db;Cache=Shared;Mode=ReadWriteCreate;"));

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

// apply migration fix because I'm not that familiar with SQL.
app.ApplyMigration();

// setup scalar openapi document map
app.UseSwagger(options => { options.RouteTemplate = "/openapi/{documentName}.json"; });
app.MapScalarApiReference();

// set up the cors to allow only the intranet user
app.UseCors("AllowIntranetIPRange");

app.UseAuthorization();

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(2.0))
    .HasApiVersion(new ApiVersion(3.0))
    .ReportApiVersions()
    .Build();

var groupBuilder = app.MapGroup("api/v{apiVersion:apiVersion}").WithApiVersionSet(apiVersionSet);
groupBuilder.MapAppEndpoints();
groupBuilder.MapVisioStencilEndpoints();
groupBuilder.MapVisioDocumentEndpoints();
groupBuilder.MapPDMSEndpoints();
groupBuilder.MapDebugEndpoints();

app.Run();

return;


void EnsureEnvironmentPathsExist()
{
    // 创建数据库存储路径
    if (!Directory.Exists(PathConstants.DatabasePath)) Directory.CreateDirectory(PathConstants.DatabasePath);

    // 创建临时文件存储路径
    if (!Directory.Exists(PathConstants.TmpPath)) Directory.CreateDirectory(PathConstants.TmpPath);

    // 创建安装包存储路径
    if (!Directory.Exists(PathConstants.InstallerPath)) Directory.CreateDirectory(PathConstants.InstallerPath);

    // 创建StencilSnapshot存储路径
    if (!Directory.Exists(PathConstants.StencilPath)) Directory.CreateDirectory(PathConstants.StencilPath);
}