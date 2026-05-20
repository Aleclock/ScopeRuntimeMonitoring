using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

public class MonitoringRegistry
{
    private readonly List<IMonitorHandle> _handles = new List<IMonitorHandle>();
    private readonly Dictionary<object, List<IMonitorHandle>> _targetHandles = new Dictionary<object, List<IMonitorHandle>>();
    private readonly object _sync = new object();
    public IReadOnlyList<IMonitorHandle> GetMonitorHandles()
    {
        lock (_sync)
        {
            // Return a snapshot to avoid external mutation/race issues.
            return _handles.ToArray();
        }
    } 

    
    public void RegisterTarget(object target)
    {
        if (target == null) return;

        lock (_sync)
        {
            if (_targetHandles.ContainsKey(target))
                return; // Already registered
        }

        var handles = new List<IMonitorHandle>();

        DiscoverFields(target, handles);
        DiscoverProperties(target, handles);

        if (handles.Count > 0)
        {
            _targetHandles[target] = handles;
            _handles.AddRange(handles);
        }
        else
        {
            // Still track target to prevent repeated discovery attempts.
            _targetHandles[target] = handles;
        }
    }

    private void DiscoverFields(object target, List<IMonitorHandle> destination)
    {
        var type = target.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (!field.IsDefined(typeof(MonitorAttribute), true))
                continue;

            destination.Add(new FieldMonitorHandle(target, field));
        }
    }

    private void DiscoverProperties(object target, List<IMonitorHandle> destination)
    {
        Type type = target.GetType();
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var p in properties)
        {
            if (!p.IsDefined(typeof(MonitorAttribute), true))
                continue;
            
            destination.Add(new PropertyMonitorHandle(target, p));
        }
    }

    public void UnregisterTarget(object target)
    {
        if (target == null) return;

        lock (_sync)
        {
            if (!_targetHandles.TryGetValue(target, out var handles))
                return; // Not registered
            
            foreach (var h in handles)
                _handles.Remove(h);
            
            _targetHandles.Remove(target);
        }
    }
}