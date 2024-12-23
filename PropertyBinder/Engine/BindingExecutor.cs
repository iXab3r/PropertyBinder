using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using PropertyBinder.Diagnostics;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine;

internal abstract class BindingExecutor
{
#if DEBUG
    /// <summary>
    /// Allows to track created BindingExecutor instances, needed mostly for debugging purposes
    /// </summary>
    public static readonly ConcurrentDictionary<int, BindingExecutor> ExecutorsByManagedThreadId = new();
#endif

    [ThreadStatic] private static BindingExecutor _instance;

    private static EventHandler<BindingExceptionEventArgs> _executorExceptionHandler;

    protected static IBindingTracer _tracer;

    private static BindingExecutor Instance
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _instance ?? ResetInstance();
    }

    public static BindingExecutor ResetInstance()
    {
        _instance = new ImmediateBindingExecutor();

#if DEBUG
        ExecutorsByManagedThreadId[Thread.CurrentThread.ManagedThreadId] = _instance;
#endif

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
    internal static void ExecuteImmediate<TContext>(
        Binder<TContext> binder,
        TContext context,
        IWatcherRoot watcher,
        IReadOnlyList<Binder<TContext>.BindingAction> actions) where TContext : class
    {
        for (var index = 0; index < actions.Count; index++)
        {
            var action = actions[index];
            if (action == null)
            {
                continue;
            }

            if (!action.RunOnAttach)
            {
                continue;
            }

            try
            {
                action.Action(context);
            }
            catch (Exception ex)
            {
                var debugDetails = new {StampExpression = action.StampExpression.ToString(), StampInvokeResult = action.GetStamped(context), Context = context}.ToString();
                var exception = new BindingException($"Binder exception on Attach, details: {debugDetails} - {ex}", ex);
                var eventArgs = new BindingExceptionEventArgs(exception, debugDetails);

                binder.HandleException(eventArgs);
                if (!eventArgs.Handled)
                {
                    throw exception;
                }
            }
        }
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