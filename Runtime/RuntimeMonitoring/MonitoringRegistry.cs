using System;
using System.Collections.Generic;
using System.Reflection;

public class MonitoringRegistry
{
    private readonly List<IMonitorHandle> _handles = new List<IMonitorHandle>();
    private readonly Dictionary<object, List<IMonitorHandle>> _targetHandles = new Dictionary<object, List<IMonitorHandle>>();
    private readonly object _sync = new object();

    public IReadOnlyList<IMonitorHandle> GetMonitorHandles()
    {
        lock (_sync)
        {
            return _handles.ToArray();
        }
    }

    public bool RegisterTarget(object target)
    {
        if (target == null) return false;

        lock (_sync)
        {
            if (_targetHandles.ContainsKey(target))
                return _targetHandles[target].Count > 0;
        }

        var handles = new List<IMonitorHandle>();

        DiscoverFields(target, handles);
        DiscoverProperties(target, handles);

        if (handles.Count > 0)
        {
            _targetHandles[target] = handles;
            _handles.AddRange(handles);
            return true;
        }

        _targetHandles[target] = handles;
        return false;
    }

    private void DiscoverFields(object target, List<IMonitorHandle> destination)
    {
        var type = target.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<MonitorAttribute>(true);
            if (attribute == null || !attribute.Enabled)
                continue;

            destination.Add(new FieldMonitorHandle(target, field, attribute));
        }
    }

    private void DiscoverProperties(object target, List<IMonitorHandle> destination)
    {
        var type = target.GetType();
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<MonitorAttribute>(true);
            if (attribute == null || !attribute.Enabled)
                continue;

            destination.Add(new PropertyMonitorHandle(target, property, attribute));
        }
    }

    public void UnregisterTarget(object target)
    {
        if (target == null) return;

        lock (_sync)
        {
            if (!_targetHandles.TryGetValue(target, out var handles))
                return;

            foreach (var handle in handles)
                _handles.Remove(handle);

            _targetHandles.Remove(target);
        }
    }
}