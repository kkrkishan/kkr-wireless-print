using System.Collections.Concurrent;
using System.Threading.Channels;
using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.Core.Services;

public class JobQueueService : IJobQueueService, IDisposable
{
    private readonly IPrintProcessor _printProcessor;
    private readonly ConcurrentDictionary<Guid, PrintJobRecord> _jobs = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationSources = new();
    private readonly Channel<PrintJobRecord> _channel = Channel.CreateUnbounded<PrintJobRecord>();

    private CancellationTokenSource? _workerCts;
    private Task? _workerTask;
    private bool _isDisposed;

    public event Action<PrintJobRecord>? JobUpdated;

    public JobQueueService(IPrintProcessor printProcessor)
    {
        _printProcessor = printProcessor;
    }

    public void Start()
    {
        if (_workerTask != null) return;

        _workerCts = new CancellationTokenSource();
        _workerTask = Task.Run(() => ProcessQueueLoopAsync(_workerCts.Token));
        AppLogger.Instance.Info("Queue", "Print job queue processing worker started.");
    }

    public void Stop()
    {
        if (_workerCts != null)
        {
            _workerCts.Cancel();
            _workerTask?.Wait(TimeSpan.FromSeconds(3));
            _workerCts.Dispose();
            _workerCts = null;
            _workerTask = null;
            AppLogger.Instance.Info("Queue", "Print job queue processing worker stopped.");
        }
    }

    public PrintJobRecord EnqueueJob(PrintJobRecord record)
    {
        record.Status = JobStatus.Queued;
        record.StatusMessage = "Job queued for printing.";
        _jobs[record.JobId] = record;

        var jobCts = new CancellationTokenSource();
        _cancellationSources[record.JobId] = jobCts;

        _channel.Writer.TryWrite(record);
        NotifyJobUpdated(record);

        AppLogger.Instance.Info("Queue", $"Enqueued job {record.JobId} for printer '{record.PrinterName}'. Total in history: {_jobs.Count}");
        return record;
    }

    public PrintJobRecord? GetJob(Guid jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public IReadOnlyList<PrintJobRecord> GetAllJobs()
    {
        return _jobs.Values.OrderByDescending(j => j.CreatedAtUtc).ToList();
    }

    public bool CancelJob(Guid jobId, out string? reason)
    {
        reason = null;
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            reason = "Job not found.";
            return false;
        }

        if (job.Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled)
        {
            reason = $"Job is already in {job.Status} state.";
            return false;
        }

        if (_cancellationSources.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
        }

        if (job.WindowsSpoolerJobId.HasValue)
        {
            NativePrintSpooler.CancelSpoolerJob(job.PrinterName, job.WindowsSpoolerJobId.Value, out var spoolErr);
            if (spoolErr != null)
            {
                AppLogger.Instance.Warning("Queue", $"Spooler cancel warning: {spoolErr}");
            }
        }

        job.Status = JobStatus.Cancelled;
        job.StatusMessage = "Job cancelled by user.";
        job.CompletedAtUtc = DateTime.UtcNow;
        NotifyJobUpdated(job);
        AppLogger.Instance.Info("Queue", $"Job {jobId} cancelled successfully.");
        return true;
    }

    private async Task ProcessQueueLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var record = await _channel.Reader.ReadAsync(stoppingToken);

                if (record.Status == JobStatus.Cancelled)
                {
                    continue;
                }

                _cancellationSources.TryGetValue(record.JobId, out var jobCts);
                var linkedCts = jobCts != null
                    ? CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, jobCts.Token)
                    : CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                try
                {
                    record.Status = JobStatus.Processing;
                    NotifyJobUpdated(record);

                    await _printProcessor.ProcessPrintJobAsync(record, linkedCts.Token);
                }
                finally
                {
                    _cancellationSources.TryRemove(record.JobId, out _);
                    linkedCts.Dispose();
                    NotifyJobUpdated(record);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error("Queue", "Unexpected error in queue loop", ex);
            }
        }
    }

    private void NotifyJobUpdated(PrintJobRecord record)
    {
        try
        {
            JobUpdated?.Invoke(record);
        }
        catch
        {
            // Ignore subscriber errors
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            Stop();
            _isDisposed = true;
        }
    }
}
