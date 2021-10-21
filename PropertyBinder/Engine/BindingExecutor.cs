using System;
using System.Runtime.CompilerServices;
using PropertyBinder.Diagnostics;

namespace PropertyBinder.Engine
{
    internal abstract class BindingExecutor
    {
        [ThreadStatic]
        private static BindingExecutor _instance;
        
        private static EventHandler<ExceptionEventArgs> _exceptionHandler;
        protected static IBindingTracer _tracer;

        private static BindingExecutor Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _instance ?? ResetInstance();
        }

        public static BindingExecutor ResetInstance()
        {
            _instance = Binder.SupportTransactions 
                ? Binder.DebugMode || _tracer != null ? new DebugModeBindingExecutor() : new ProductionModeBindingExecutor()
                : new ImmediateBindingExecutor();
            return _instance;
        }

        public static void SetTracer(IBindingTracer tracer)
        {
            _tracer = tracer;
            ResetInstance();
        }

        public static void SetExceptionHandler(EventHandler<ExceptionEventArgs> exceptionHandler)
        {
            _exceptionHandler = exceptionHandler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Execute(BindingMap map, int[] bindings)
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

        protected abstract void ExecuteInternal(BindingMap map, int[] bindings);

        protected abstract void SuspendInternal();

        protected abstract void ResumeInternal();

        protected virtual void HandleExecutionException(Exception ex, BindingReference binding)
        {
            string stampResult;
            try 
            {
                stampResult = binding.GetStamp();
            }
            catch(Exception stampEx)
            {
                stampResult = $"Failed to get stamp: {stampEx}";
            }

            DebugContext debugContext;
            try 
            {
                debugContext = binding.DebugContext;
            }
            catch(Exception)
            {
                debugContext = default;
            }
                        
            var exception = new BindingException($"BindingExecutor exception, description: {debugContext?.Description}, stamp: {stampResult} - {ex}", ex);
            var exceptionEventArgs = new ExceptionEventArgs(exception, stampResult, debugContext);
            _exceptionHandler?.Invoke(null, exceptionEventArgs);
            if (!exceptionEventArgs.Handled)
            {
                throw exception;
            }
        }
    }
}