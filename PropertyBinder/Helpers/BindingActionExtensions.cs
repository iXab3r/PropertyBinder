using System;

namespace PropertyBinder.Helpers;

internal static class BindingActionExtensions
{
    public static string GetStamped<TContext>(this Binder<TContext>.BindingAction action, TContext context) where TContext : class
    {
        string stampResult;
        try 
        {
            var stamped = ExpressionHelpers.Stamped<TContext>(action?.StampExpression);
            stampResult = stamped?.Invoke(context) ?? "NULL";
        }
        catch(Exception stampEx)
        {
            stampResult = $"Failed: {stampEx}";
        }
        return stampResult;
    }
}