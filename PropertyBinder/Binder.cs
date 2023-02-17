﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using PropertyBinder.Diagnostics;
using PropertyBinder.Engine;
using PropertyBinder.Helpers;
using PropertyBinder.Visitors;

namespace PropertyBinder;

public sealed class Binder<TContext>
    where TContext : class
{
    private readonly IBindingNodeBuilder<TContext> _rootNode;
    private readonly List<BindingAction> _actions;
    private IWatcherFactory<TContext> _factory;
    private BindingAction[] _compactedActions;
    private EventHandler<BindingExceptionEventArgs> _binderExceptionHandler;

    private Binder(IBindingNodeBuilder<TContext> rootNode, List<BindingAction> actions)
    {
        _rootNode = rootNode;
        _actions = actions;
    }

    public Binder()
        : this(new BindingNodeRootBuilder<TContext>(), new List<BindingAction>())
    {
    }

    public Binder<TNewContext> Clone<TNewContext>()
        where TNewContext : class, TContext
    {
        return new Binder<TNewContext>(
            _rootNode.CloneForDerivedParentType<TNewContext>(),
            _actions.Select(x => x != null ? new Binder<TNewContext>.BindingAction(x.Action, x.Key, x.DebugContext, x.RunOnAttach, x.StampExpression) : null).ToList());
    }

    public Binder<TContext> WithExceptionHandler(EventHandler<BindingExceptionEventArgs> exceptionHandler)
    {
        if (_binderExceptionHandler != null)
        {
            throw new InvalidOperationException($"Exception handler is already set for {this}");
        }
        _binderExceptionHandler = exceptionHandler;
        return this;
    }

    public Binder<TContext> Clone()
    {
        return Clone<TContext>();
    }

    public IDisposable BeginTransaction()
    {
        return Binder.BeginTransaction();
    }

    internal void AddRule(Action<TContext> bindingAction, string key, DebugContext debugContext, bool runOnAttach, bool canOverride,
        Expression stampExpression, IEnumerable<Expression> triggerExpressions)
    {
        if (_factory != null)
        {
            throw new InvalidOperationException("This binder already has attached watchers. It cannot be modified, but you can clone it and modify the clone instead.");
        }

        if (!string.IsNullOrEmpty(key) && canOverride)
        {
            RemoveRule(key);
        }

        _actions.Add(new BindingAction(bindingAction, key, debugContext, runOnAttach, stampExpression));

        foreach (var expr in triggerExpressions)
        {
            new BindingExpressionVisitor<TContext>(_rootNode, typeof(TContext), _actions.Count - 1).Visit(expr);
        }
    }

    internal void RemoveRule(string key)
    {
        if (_factory != null)
        {
            throw new InvalidOperationException("This binder already has attached watchers. It cannot be modified, but you can clone it and modify the clone instead.");
        }

        for (int i = 0; i < _actions.Count; ++i)
        {
            if (_actions[i] != null && _actions[i].Key == key)
            {
                _actions[i] = null;
            }
        }
    }

    public Action<TContext> GetActionByKey(string key)
    {
        Action<TContext> result = null;

        for (int i = 0; i < _actions.Count; ++i)
        {
            if (_actions[i] != null && _actions[i].Key == key)
            {
                result += _actions[i].Action;
            }
        }

        return result ?? (_ => { });
    }

    public IDisposable Attach(TContext context)
    {
        if (_factory == null)
        {
            var remap = new int[_actions.Count];
            var compactedActions = new List<BindingAction>();
            for (int i = 0; i < _actions.Count; ++i)
            {
                var action = _actions[i];
                if (action != null)
                {
                    remap[i] = compactedActions.Count;
                    compactedActions.Add(action);
                }
                else
                {
                    remap[i] = -1;
                }
            }

            _compactedActions = compactedActions.ToArray();

            _factory = Binder.AllowReuseOfWatchers
                ? new ReusableWatcherFactory<TContext>(_compactedActions, _rootNode.CreateBindingNode(remap))
                : new DefaultWatcherFactory<TContext>(_actions.ToArray(), _rootNode.CreateBindingNode(remap));
        }

        var watcher = _factory.Attach(context);

        lock (watcher.Map)
        {
            for (var index = 0; index < _compactedActions.Length; index++)
            {
                var action = _compactedActions[index];
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

                    _binderExceptionHandler?.Invoke(this, eventArgs);
                    if (!eventArgs.Handled)
                    {
                        Binder.ExceptionHandler?.Invoke(this, eventArgs);
                        if (!eventArgs.Handled)
                        {
                            throw exception;
                        }
                    }
                }
            }
        }

        return watcher;
    }

    internal sealed class BindingAction
    {
        public BindingAction(Action<TContext> action, string key, DebugContext debugContext, bool runOnAttach, Expression stampExpression)
        {
            Action = action;
            Key = key;
            DebugContext = debugContext;
            RunOnAttach = runOnAttach;
            StampExpression = stampExpression;
        }

        public readonly Action<TContext> Action;

        public readonly bool RunOnAttach;

        public readonly string Key;

        public readonly DebugContext DebugContext;

        public Expression StampExpression;
    }
}

public static class Binder
{
    private static bool _debugMode = Debugger.IsAttached;
    private static bool _supportTransactions = false;

    private sealed class BindingTransaction : IDisposable
    {
        public BindingTransaction()
        {
            BindingExecutor.Suspend();
        }

        public void Dispose()
        {
            BindingExecutor.Resume();
        }
    }

    public static IDisposable BeginTransaction()
    {
        return new BindingTransaction();
    }
        
    public static void SetTracer(IBindingTracer tracer)
    {
        BindingExecutor.SetTracer(tracer);
    }

    public static void SetExceptionHandler(EventHandler<BindingExceptionEventArgs> exceptionHandler)
    {
        ExceptionHandler = exceptionHandler;
        BindingExecutor.SetExceptionHandler(exceptionHandler);
    }

    public static bool DebugMode
    {
        get => _debugMode;
        set
        {
            if (_debugMode != value)
            {
                _debugMode = value;
                BindingExecutor.ResetInstance();
            }
        }
    }
        
    public static bool SupportTransactions
    {
        get => _supportTransactions;
        set
        {
            if (_supportTransactions != value)
            {
                _supportTransactions = value;
                BindingExecutor.ResetInstance();
            }
        }
    }
        
    internal static EventHandler<BindingExceptionEventArgs> ExceptionHandler { get; private set; }
        
    public static IExpressionCompiler ExpressionCompiler { get; set; } = DefaultExpressionCompiler.Instance;

    public static CommandCanExecuteCheckMode DefaultCommandCanExecuteCheckMode { get; set; } = CommandCanExecuteCheckMode.DoNotCheck;

    public static bool AllowReuseOfWatchers { get; set; } = true;
}