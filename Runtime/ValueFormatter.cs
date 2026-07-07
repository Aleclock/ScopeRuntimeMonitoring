namespace ScopeRuntimeMonitoring
{
    public static class ValueFormatter
    {
        public static string FormatValue(object value)
        {
            if (value == null) return "null";
            var t = value.GetType();
            var processor = ValueProcessorRegistry.GetProcessor(t);
            if (processor != null) return processor(value);
            return value.ToString();
        }
    }
}