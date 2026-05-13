using Microsoft.EntityFrameworkCore;
using Rira.Akbaritabar.Test.Server.Data;
using Rira.Akbaritabar.Test.Server.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=app.db");
});

WebApplication app = builder.Build();

app.MapGrpcService<PersonGrpcService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
