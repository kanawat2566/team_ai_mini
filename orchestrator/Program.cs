using Microsoft.EntityFrameworkCore;
using Orchestrator.Data;
using Orchestrator.Hubs;
using Orchestrator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrchestratorDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=orchestrator.db"));

builder.Services.Configure<GithubWebhookOptions>(builder.Configuration.GetSection("GithubWebhook"));
builder.Services.AddScoped<SignatureVerifier>();
builder.Services.AddScoped<FlowStateMapper>();
builder.Services.AddScoped<FlowEventIngestor>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<FlowHub>("/hubs/flow");

app.Run();
