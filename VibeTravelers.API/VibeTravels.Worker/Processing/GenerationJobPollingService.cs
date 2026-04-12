using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using VibeTravels.Infrastructure.Persistence;
using VibeTravels.Worker.Options;

namespace VibeTravels.Worker.Processing;

public sealed class GenerationJobPollingService
{
    private readonly AppDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<GenerationWorkerOptions> _optionsMonitor;
    private readonly ILogger<GenerationJobPollingService> _logger;

    public GenerationJobPollingService(
        AppDbContext db,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<GenerationWorkerOptions> optionsMonitor,
        ILogger<GenerationJobPollingService> logger)
    {
        _db = db;
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var maxParallel = Math.Max(1, options.MaxParallelJobs);
        var batchSize = Math.Max(1, Math.Min(options.BatchSize, maxParallel));
        _db.Database.SetCommandTimeout(TimeSpan.FromSeconds(Math.Max(1, options.CommandTimeoutSeconds)));

        await RecoverStaleRunningJobsAsync(cancellationToken);
        var claimedJobIds = await ClaimPendingJobsAsync(batchSize, cancellationToken);
        if (claimedJobIds.Count == 0)
            return 0;

        var tasks = claimedJobIds
            .Select(jobId => ProcessSingleJobInIsolatedScopeAsync(jobId, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);
        return claimedJobIds.Count;
    }

    private async Task RecoverStaleRunningJobsAsync(CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var maxAttempts = Math.Max(1, options.MaxAttempts);
        var staleThreshold = DateTimeOffset.UtcNow.AddMinutes(-Math.Max(1, options.StaleProcessingAfterMinutes));
        var now = DateTimeOffset.UtcNow;

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE generation_jobs
            SET status = 'pending',
                started_at = NULL,
                error_code = {GenerationJobErrorCodes.WorkerStaleJob},
                error_message = 'Job was stale and moved back to pending.'
            WHERE status = 'running'
              AND finished_at IS NULL
              AND started_at IS NOT NULL
              AND started_at < {staleThreshold}
              AND attempt_no < {maxAttempts};
            """,
            cancellationToken);

        var failedCount = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE generation_jobs
            SET status = 'failed',
                finished_at = {now},
                error_code = {GenerationJobErrorCodes.WorkerStaleJob},
                error_message = 'Job was stale and max attempts were exhausted.'
            WHERE status = 'running'
              AND finished_at IS NULL
              AND started_at IS NOT NULL
              AND started_at < {staleThreshold}
              AND attempt_no >= {maxAttempts};
            """,
            cancellationToken);

        if (failedCount > 0)
        {
            _logger.LogWarning("Recovered {FailedCount} stale running jobs as failed.", failedCount);
        }
    }

    private async Task<IReadOnlyList<Guid>> ClaimPendingJobsAsync(int batchSize, CancellationToken cancellationToken)
    {
        const string sql = """
                           WITH picked AS (
                               SELECT id
                               FROM generation_jobs
                               WHERE status = 'pending'
                               ORDER BY requested_at ASC
                               FOR UPDATE SKIP LOCKED
                               LIMIT @batchSize
                           )
                           UPDATE generation_jobs AS gj
                           SET status = 'running',
                               started_at = @nowUtc,
                               attempt_no = gj.attempt_no + 1,
                               error_code = NULL,
                               error_message = NULL
                           FROM picked
                           WHERE gj.id = picked.id
                           RETURNING gj.id;
                           """;

        var ids = new List<Guid>();
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        command.Parameters.Add(new NpgsqlParameter("@batchSize", batchSize));
        command.Parameters.Add(new NpgsqlParameter("@nowUtc", DateTimeOffset.UtcNow));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    private async Task ProcessSingleJobInIsolatedScopeAsync(Guid jobId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<GenerationJobProcessor>();
            await processor.ProcessAsync(jobId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Processing failed for generation job {JobId}.", jobId);
        }
    }
}
