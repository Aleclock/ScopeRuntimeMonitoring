using System;

// TODO in the future I might need adding parameters like format, tags, processorName, ...
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public class MonitorAttribute : Attribute
{
    public string Label; 
    public string Format { get; set; }
    public string[] Tags { get; set; }
    public bool Enabled { get; set; } = true;
    public MonitorAttribute(string label)
    {
        Label = label;
    }
}