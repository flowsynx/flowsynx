namespace FlowSynx.Services;

public class JobQueueService : IHostedService
{
    private readonly IJobQueue _jobQueue;

    //private readonly BlockingCollection<Func<CancellationToken, Task<object>>> _jobQueue;
    //private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> _jobResults;
    private readonly CancellationTokenSource _serviceCancellationTokenSource;
    private Task _workerTask;
    private bool _isPaused;

    public JobQueueService(IJobQueue jobQueu)
    {
        _jobQueue = jobQueu;
        //_jobQueue = new BlockingCollection<Func<CancellationToken, Task<object>>>();
        //_jobResults = new ConcurrentDictionary<Guid, TaskCompletionSource<object>>();
        _serviceCancellationTokenSource = new CancellationTokenSource();
        _isPaused = false;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _workerTask = Task.Run(() => ProcessJobsAsync(_serviceCancellationTokenSource.Token), cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _serviceCancellationTokenSource.Cancel();
        _jobQueue.TaskQueue.CompleteAdding();
        return _workerTask ?? Task.CompletedTask;
    }

    //public Guid EnqueueJob(Func<CancellationToken, Task<object>> job)
    //{
    //    var jobId = Guid.NewGuid();
    //    var jobResult = new TaskCompletionSource<object>();
    //    _jobResults[jobId] = jobResult;
    //    _jobQueue.Add(job);
    //    return jobId;
    //}

    //public async Task<object> GetJobResultAsync(Guid jobId)
    //{
    //    if (_jobQueue.TaskResults.TryGetValue(jobId, out var jobResult))
    //    {
    //        return await jobResult.Task;
    //    }
    //    throw new InvalidOperationException("Job ID not found.");
    //}

    //public void Pause()
    //{
    //    _isPaused = true;
    //}

    //public void Resume()
    //{
    //    _isPaused = false;
    //}

    //public void Cancel()
    //{
    //    _serviceCancellationTokenSource.Cancel();
    //}

    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var job in _jobQueue.TaskQueue.GetConsumingEnumerable(cancellationToken))
            {
                while (_isPaused)
                {
                    await Task.Delay(100, cancellationToken); // Wait for resume
                }

                var jobId = Guid.Empty;

                try
                {
                    var result = await job(cancellationToken);
                    var completedJob = _jobQueue.TaskResults.FirstOrDefault(kvp => kvp.Value.Task.IsCompleted == false);
                    jobId = completedJob.Key;

                    if (jobId != Guid.Empty)
                        _jobQueue.TaskResults[jobId].SetResult(result);
                }
                catch (OperationCanceledException)
                {
                    if (jobId != Guid.Empty && _jobQueue.TaskResults.ContainsKey(jobId))
                        _jobQueue.TaskResults[jobId].SetCanceled();
                }
                catch (Exception ex)
                {
                    if (jobId != Guid.Empty && _jobQueue.TaskResults.ContainsKey(jobId))
                        _jobQueue.TaskResults[jobId].SetException(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        finally
        {
            // Complete pending jobs
            foreach (var kvp in _jobQueue.TaskResults)
            {
                if (!kvp.Value.Task.IsCompleted)
                {
                    kvp.Value.SetCanceled();
                }
            }
        }
    }
}
