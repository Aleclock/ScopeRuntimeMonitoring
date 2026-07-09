using System;

namespace ScopeRuntimeMonitoring
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MonitorAttribute : Attribute
    {
        public string Label { get; }

        public string Id { get; set; }
        public string Group { get; set; }
        public string SubGroup { get; set; }
        public string Variant { get; set; }
        public MonitorWidgetType WidgetType { get; set; } = MonitorWidgetType.Value;
        public bool Editable { get; set; }
        public float Min { get; set; } = 0f;
        public float Max { get; set; } = 1f;
        public float Step { get; set; } = 0.1f;
        public bool Enabled { get; set; } = true;

        public MonitorAttribute(string label)
        {
            Label = label;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class MonitorSliderAttribute : MonitorAttribute
    {
        public MonitorSliderAttribute(string label, float min = 0f, float max = 1f, float step = 0.1f)
            : base(label)
        {
            WidgetType = MonitorWidgetType.Slider;
            Min = min;
            Max = max;
            Step = step;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class MonitorToggleAttribute : MonitorAttribute
    {
        public MonitorToggleAttribute(string label)
            : base(label)
        {
            WidgetType = MonitorWidgetType.Toggle;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class MonitorValueAttribute : MonitorAttribute
    {
        public MonitorValueAttribute(string label)
            : base(label)
        {
            WidgetType = MonitorWidgetType.Value;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class MonitorInputValueAttribute : MonitorAttribute
    {
        public MonitorInputValueAttribute(string label)
            : base(label)
        {
            WidgetType = MonitorWidgetType.InputValue;
            Editable = true;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class MonitorProgressAttribute : MonitorAttribute
    {
        public MonitorProgressAttribute(string label, float min = 0f, float max = 1f)
            : base(label)
        {
            WidgetType = MonitorWidgetType.Progress;
            Min = min;
            Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class MonitorWithChartAttribute : MonitorAttribute
    {
        public MonitorWithChartAttribute(string label)
            : base(label)
        {
            WidgetType = MonitorWidgetType.Custom;
            Variant = "chart";
        }
    }
}