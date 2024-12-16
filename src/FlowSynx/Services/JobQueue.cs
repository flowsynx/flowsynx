using System.Collections.Concurrent;

namespace FlowSynx.Services;

public class JobQueue : IJobQueue
{
    private readonly BlockingCollection<Func<CancellationToken, Task<object>>> _taskQueue = new();
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> _taskResults = new();

    public BlockingCollection<Func<CancellationToken, Task<object>>> TaskQueue => _taskQueue;
    public ConcurrentDictionary<Guid, TaskCompletionSource<object>> TaskResults => _taskResults;

    //public Task EnqueueTask(Func<CancellationToken, Task> job)
    //{
    //    _taskQueue.Add(job);
    //    return Task.CompletedTask;
    //}

    public Guid EnqueueTask(Func<CancellationToken, Task<object>> job)
    {
        var jobId = Guid.NewGuid();
        var jobResult = new TaskCompletionSource<object>();
        _taskResults[jobId] = jobResult;
        _taskQueue.Add(job);
        return jobId;
    }

    public async Task<object> GetJobResultAsync(Guid jobId)
    {
        if (_taskResults.TryGetValue(jobId, out var jobResult))
        {
            return await jobResult.Task;
        }
        throw new InvalidOperationException("Job ID not found.");
    }
}