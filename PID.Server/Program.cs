using System.Text.Json.Serialization;
using AE.PID.Server.Data;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

// initialize the environment
if (!Directory.Exists("/opt/pid/data")) Directory.CreateDirectory("/opt/pid/data");
if (!Directory.Exists("/opt/pid/data/apps")) Directory.CreateDirectory("/opt/pid/data/apps");
if (!Directory.Exists("/opt/pid/data/libraries")) Directory.CreateDirectory("/opt/pid/data/libraries");

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSwaggerGen(c =>
{
    // Define multiple Swagger documents for different API versions
    var apiVersionDescriptions = new[] { "v1", "v2" }; // Replace with your actual API versions
    foreach (var version in apiVersionDescriptions)
        c.SwaggerDoc(version, new OpenApiInfo
        {
            Version = version,
            Title = $"API {version}",
            Description = $"API Documentation for version {version}",
            TermsOfService = new Uri("https://example.com/terms"),
            Contact = new OpenApiContact
            {
                Name = "Your Name",
                Email = "your-email@example.com",
                Url = new Uri("https://example.com/contact")
            },
            License = new OpenApiLicense
            {
                Name = "Use under LICX",
                Url = new Uri("https://example.com/license")
            }
        });
});


builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=/opt/pid/data/PID_server.db;"));

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowIntranetIPRange",
        policy => policy.WithOrigins("http://172.18.0.0/22").AllowAnyMethod());
});

builder.Services.AddHttpClient("PDMS",
    client => { client.BaseAddress = new Uri("http://172.18.168.57:8000/api/cube/restful/interface/"); });

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
    var apiVersionDescriptions = new[] { "v1", "v2" };
    foreach (var version in apiVersionDescriptions)
        options.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"API {version}");
    options.RoutePrefix = string.Empty; // 
});

app.UseAuthorization();

app.MapControllers();

app.Run();

static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
{
    var info = new OpenApiInfo
    {
        //标题
        Title = $".NET Core API for 测试项目 {description.ApiVersion}",
        //当前版本
        Version = description.ApiVersion.ToString()
    };
    //当有弃用标记时的提示信息
    if (description.IsDeprecated) info.Description += " - 此版本已放弃兼容";
    return info;
}