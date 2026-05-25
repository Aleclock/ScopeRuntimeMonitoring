using System;

public interface IMonitorHandle
{
    string Id { get; }
    string Name { get; }
    object Target { get; }
    Type ValueType { get; }
    MonitorWidgetMetadata Metadata { get; }
    object GetValueRaw();
    string GetValueString();
    bool Enabled { get; set; }
}