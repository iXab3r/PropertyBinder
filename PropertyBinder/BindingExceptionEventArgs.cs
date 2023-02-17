using System;
using PropertyBinder.Diagnostics;

namespace PropertyBinder;

public class BindingExceptionEventArgs : EventArgs
{
    internal BindingExceptionEventArgs(Exception ex, string stampedStr, DebugContext bindingDebugContext = null)
    {
        Exception = ex;
        Description = bindingDebugContext?.Description;
        StampedStr = stampedStr;
    }

    public string Description { get; }
    public Exception Exception { get; }
    public string StampedStr { get; }
    public bool Handled { get; set; }

    public override string ToString()
    {
        return $"{nameof(Exception)}: {Exception}, {nameof(Description)}: {Description}, {nameof(StampedStr)}: {StampedStr}, {nameof(Handled)}: {Handled}";
    }
}