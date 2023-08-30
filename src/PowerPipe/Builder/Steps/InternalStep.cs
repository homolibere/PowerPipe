using System;
using System.Threading;
using System.Threading.Tasks;
using PowerPipe.Interfaces;

namespace PowerPipe.Builder.Steps;

internal abstract class InternalStep<TContext> : IPipelineStep<TContext>
{
    public IPipelineStep<TContext> NextStep { get; set; }

    protected virtual PipelineStepErrorHandling? ErrorHandlingBehaviour { get; private set; }

    protected virtual TimeSpan? RetryInterval { get; private set; }

    protected virtual int? MaxRetryCount { get; private set; }

    private int RetryCount { get; set; }

    public void ConfigureErrorHandling(PipelineStepErrorHandling errorHandling, TimeSpan? retryInterval, int? maxRetryCount)
    {
        ErrorHandlingBehaviour = errorHandling;
        RetryInterval = retryInterval;
        MaxRetryCount = maxRetryCount;
    }

    public async Task ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteInternalAsync(context, cancellationToken);
        }
        catch (Exception)
        {
            var errorHandleSucceed = await HandleExceptionAsync(context, cancellationToken);

            if(!errorHandleSucceed)
                throw;
        }
    }

    protected virtual Task ExecuteInternalAsync(TContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    protected virtual async Task<bool> HandleExceptionAsync(TContext context, CancellationToken cancellationToken)
    {
        switch (ErrorHandlingBehaviour)
        {
            case PipelineStepErrorHandling.Suppress:
                return true;

            case PipelineStepErrorHandling.Retry:
                break;

            case null:
                return false;
        }

        if (RetryCount >= (MaxRetryCount ?? 1))
            return false;

        RetryCount++;
        await Task.Delay(RetryInterval ?? TimeSpan.FromSeconds(1), cancellationToken);

        await ExecuteAsync(context, cancellationToken);

        return true;
    }
}

public enum PipelineStepErrorHandling
{
    Suppress = 0,
    Retry = 1,
}