using System.Text.Json.Serialization;
using AE.PID.Server.Data;
using Microsoft.EntityFrameworkCore;

// initialize the environment
if (!Directory.Exists("/opt/pid/data")) Directory.CreateDirectory("/opt/pid/data");
if (!Directory.Exists("/opt/pid/data/apps")) Directory.CreateDirectory("/opt/pid/data/apps");
if (!Directory.Exists("/opt/pid/data/libraries")) Directory.CreateDirectory("/opt/pid/data/libraries");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // to involve cycle reference, see https://learn.microsoft.com/zh-cn/ef/core/querying/related-data/serialization
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=/opt/pid/data/PID_server.db;"));

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

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
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();