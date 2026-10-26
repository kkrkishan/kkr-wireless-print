using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.Core.Services;

public interface IJobQueueService
{
    event Action<PrintJobRecord>? JobUpdated;

    PrintJobRecord EnqueueJob(PrintJobRecord record);
    PrintJobRecord? GetJob(Guid jobId);
    IReadOnlyList<PrintJobRecord> GetAllJobs();
    bool CancelJob(Guid jobId, out string? reason);
    void Start();
    void Stop();
}
