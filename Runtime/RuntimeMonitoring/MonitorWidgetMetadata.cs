using System;
using System.Reflection;

namespace ScopeRuntimeMonitoring
{
    public readonly struct MonitorWidgetMetadata
    {
        public readonly string Id;
        public readonly string Label;
        public readonly string Group;
        public readonly string SubGroup;
        public readonly string Variant;
        public readonly MonitorWidgetType WidgetType;
        public readonly bool Editable;
        public readonly float Min;
        public readonly float Max;
        public readonly float Step;
        public readonly bool Enabled;
        public readonly int Order;
        public readonly int SubGroupOrder;

        public MonitorWidgetMetadata(
            string id,
            string label,
            string group,
            string subGroup,
            string variant,
            MonitorWidgetType widgetType,
            bool editable,
            float min,
            float max,
            float step,
            bool enabled,
            int order,
            int subGroupOrder)
        {
            Id = id;
            Label = label;
            Group = group;
            SubGroup = subGroup;
            Variant = variant;
            WidgetType = widgetType;
            Editable = editable;
            Min = min;
            Max = max;
            Step = step;
            Enabled = enabled;
            Order = order;
            SubGroupOrder = subGroupOrder;
        }

        public static MonitorWidgetMetadata From(object target, MemberInfo member, MonitorAttribute attribute)
        {
            var fallbackLabel = member?.Name ?? string.Empty;
            var label = attribute != null && !string.IsNullOrWhiteSpace(attribute.Label)
                ? attribute.Label
                : fallbackLabel;

            var stableMemberName = member?.DeclaringType == null
                ? fallbackLabel
                : $"{member.DeclaringType.FullName}.{member.Name}";

            var id = attribute != null && !string.IsNullOrWhiteSpace(attribute.Id)
                ? attribute.Id
                : $"{target?.GetHashCode() ?? 0}::{stableMemberName}";

            string group = null;
            if (Monitor.GroupCustomizer != null)
            {
                group = Monitor.GroupCustomizer(target);
            }
            if (string.IsNullOrWhiteSpace(group) && target is IMonitorGroupCustomizer groupCustomizer)
            {
                group = groupCustomizer.GetMonitorGroup();
            }
            if (string.IsNullOrWhiteSpace(group) && attribute != null && !string.IsNullOrWhiteSpace(attribute.Group))
            {
                group = attribute.Group;
            }
            if (string.IsNullOrWhiteSpace(group))
            {
                group = member?.DeclaringType?.Name ?? string.Empty;
            }

            string subGroup = null;
            if (Monitor.SubGroupCustomizer != null)
            {
                subGroup = Monitor.SubGroupCustomizer(target);
            }
            if (string.IsNullOrWhiteSpace(subGroup) && target is IMonitorSubGroupCustomizer subGroupCustomizer)
            {
                subGroup = subGroupCustomizer.GetMonitorSubGroup();
            }
            if (string.IsNullOrWhiteSpace(subGroup) && attribute != null && !string.IsNullOrWhiteSpace(attribute.SubGroup))
            {
                subGroup = attribute.SubGroup;
            }
            if (string.IsNullOrWhiteSpace(subGroup))
            {
                subGroup = string.Empty;
            }

            var variant = attribute != null && !string.IsNullOrWhiteSpace(attribute.Variant)
                ? attribute.Variant
                : string.Empty;

            var widgetType = attribute != null ? attribute.WidgetType : MonitorWidgetType.Value;
            var editable = attribute != null && attribute.Editable;
            var min = attribute != null ? attribute.Min : 0f;
            var max = attribute != null ? attribute.Max : 1f;
            var step = attribute != null ? attribute.Step : 0.1f;
            var enabled = attribute == null || attribute.Enabled;
            var order = attribute != null ? attribute.Order : int.MaxValue;
            var subGroupOrder = attribute != null ? attribute.SubGroupOrder : int.MaxValue;

            return new MonitorWidgetMetadata(id, label, group, subGroup, variant, widgetType, editable, min, max, step, enabled, order, subGroupOrder);
        }
    }
}