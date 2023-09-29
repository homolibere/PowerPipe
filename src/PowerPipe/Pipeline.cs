﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PowerPipe.Interfaces;

namespace PowerPipe;

public class Pipeline<TContext, TResult> : IPipeline<TResult>
    where TContext : PipelineContext<TResult>
    where TResult : class
{
    private readonly TContext _context;
    private readonly IPipelineStep<TContext> _initStep;

    public Pipeline(TContext context, IReadOnlyList<IPipelineStep<TContext>> steps)
    {
        _context = context;

        _initStep = steps[0];

        SetupSteps(steps);
    }

    public async Task<TResult> RunAsync(CancellationToken cancellationToken, bool returnResult = true)
    {
        await _initStep.ExecuteAsync(_context, cancellationToken);

        // to avoid multiple result calls in nested pipelines
        return returnResult ? _context.GetPipelineResult() : null;
    }

    private static void SetupSteps(IReadOnlyList<IPipelineStep<TContext>> steps)
    {
        for (var i = 0; i < steps.Count - 1; i++)
        {
            steps[i].NextStep = steps[i + 1];
        }
    }
}