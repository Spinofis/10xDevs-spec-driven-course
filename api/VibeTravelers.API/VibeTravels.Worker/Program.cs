using VibeTravels.Application;
using VibeTravels.Infrastructure;
using VibeTravels.Worker.HostedServices;
using VibeTravels.Worker.Options;
using VibeTravels.Worker.Processing;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<GenerationWorkerOptions>(
    builder.Configuration.GetSection(GenerationWorkerOptions.SectionName));
builder.Services.Configure<HostOptions>(options =>
{
    var shutdownTimeoutSeconds = builder.Configuration.GetValue<int?>(
        $"{GenerationWorkerOptions.SectionName}:ShutdownTimeoutSeconds") ?? 30;
    options.ShutdownTimeout = TimeSpan.FromSeconds(Math.Max(1, shutdownTimeoutSeconds));
});

builder.Services.AddScoped<GenerationJobPollingService>();
builder.Services.AddScoped<GenerationJobProcessor>();
builder.Services.AddHostedService<JobPollingHostedService>();

var host = builder.Build();
host.Run();
