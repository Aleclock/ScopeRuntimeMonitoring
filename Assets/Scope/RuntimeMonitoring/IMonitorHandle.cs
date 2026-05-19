public interface IMonitorHandle
{
    string Name { get; }
    object Target { get; }
    string GetValueString();
}