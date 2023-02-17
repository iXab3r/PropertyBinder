using System;
using System.Runtime.CompilerServices;
using PropertyBinder.Diagnostics;

namespace PropertyBinder.Engine;

internal readonly struct BindingReference
{
    public readonly BindingMap Map;
    public readonly int Index;

    public BindingReference(BindingMap map, int index)
    {
        Map = map;
        Index = index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Schedule()
    {
        if (!Map.Schedule[Index])
        {
            Map.Schedule[Index] = true;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnSchedule()
    {
        Map.Schedule[Index] = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute()
    {
        Map.Execute(Index);
    }

    public string GetStamp()
    {
        try 
        {
            return Map.GetStamp(Index);
        }
        catch(Exception stampEx)
        {
            return $"Failed to get stamp: {stampEx}";
        }
    }

    public DebugContext DebugContext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            try 
            {
                return Map.GetDebugContext(Index);
            }
            catch(Exception)
            {
                return default;
            }
        }
    }
}