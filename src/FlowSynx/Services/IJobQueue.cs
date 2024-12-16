using System.Collections.Concurrent;

namespace FlowSynx.Services;

public interface IJobQueue
{
    public BlockingCollection<Func<CancellationToken, Task<object>>> TaskQueue { get; }
    public ConcurrentDictionary<Guid, TaskCompletionSource<object>> TaskResults { get; }

    //Guid EnqueueTask(Func<CancellationToken, Task> job);
    Guid EnqueueTask(Func<CancellationToken, Task<object>> job);
    Task<object> GetJobResultAsync(Guid jobId);
}