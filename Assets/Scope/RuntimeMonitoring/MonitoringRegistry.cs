using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

public class MonitoringRegistry
{
    private readonly List<IMonitorHandle> _handles = new List<IMonitorHandle>();
    public IReadOnlyList<IMonitorHandle> GetMonitorHandles() => _handles;
    
    public void RegisterTarget(object target)
    {
        if (target == null) return;

        // Prevents double-registration of the same target
        if (_handles.Any(h => h.Target == target))
            return;

        DiscoverFields(target);
        DiscoverProperties(target);
    }

    private void DiscoverFields(object target)
    {
        Type type = target.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach(var f in fields)
        {
            if (!f.IsDefined(typeof(MonitorAttribute), true))
                continue;

            _handles.Add(new FieldMonitorHandle(target, f));
        }
    }

    private void DiscoverProperties(object target)
    {
        Type type = target.GetType();
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var p in properties)
        {
            if (!p.IsDefined(typeof(MonitorAttribute), true))
                continue;
            
            _handles.Add(new PropertyMonitorHandle(target, p));
        }
    }

    public void UnregisterTarget(object target)
    {
        _handles.RemoveAll(handle => handle.Target == target);
    }
}