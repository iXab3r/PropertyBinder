using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PropertyBinder.Diagnostics;

namespace PropertyBinder.Engine;

internal abstract class BindingExecutor
{
    [ThreadStatic]
    private static BindingExecutor _instance;
        
    private static EventHandler<BindingExceptionEventArgs> _executorExceptionHandler;
    protected static IBindingTracer _tracer;

    private static BindingExecutor Instance
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _instance ?? ResetInstance();
    }

    public static BindingExecutor ResetInstance()
    {
        _instance = new ImmediateBindingExecutor();
        return _instance;
    }

    public static void SetTracer(IBindingTracer tracer)
    {
        _tracer = tracer;
        ResetInstance();
    }

    public static void SetExceptionHandler(EventHandler<BindingExceptionEventArgs> exceptionHandler)
    {
        _executorExceptionHandler = exceptionHandler;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Execute(BindingMap map, IReadOnlyList<int> bindings)
    {
        Instance.ExecuteInternal(map, bindings);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Suspend()
    {
        Instance.SuspendInternal();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Resume()
    {
        Instance.ResumeInternal();
    }

    protected abstract void ExecuteInternal(BindingMap map, IReadOnlyList<int> bindings);

    protected abstract void SuspendInternal();

    protected abstract void ResumeInternal();

    protected static void HandleExecutionException(Exception ex, BindingReference binding)
    {
        var stampResult = binding.GetStamp();
        var debugContext = binding.DebugContext;
        var exception = new BindingException($"BindingExecutor exception, description: {debugContext?.Description}, stamp: {stampResult} - {ex}", ex);
        var exceptionEventArgs = new BindingExceptionEventArgs(exception, stampResult, debugContext);
        
        _executorExceptionHandler?.Invoke(null, exceptionEventArgs);
        if (exceptionEventArgs.Handled)
        {
            return;
        }

        throw exception;
    }
}