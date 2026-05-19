using UnityEditor;
using UnityEngine;

public static class ValueFormatter
{
    public static string FormatValue(object value)
    {
        if (value == null)
            return "null";
        
        var type = value.GetType();

        // Float: 2 decimal places
        if (type == typeof(float))
            return ((float)value).ToString("F2");

        // Int: no change needed
        if (type == typeof(int))
            return ((int)value).ToString();

        // Bool: use text representation
        if (type == typeof(bool))
            return ((bool)value) ? "true" : "false";
        
        // Vector3: compact format
        if (type == typeof(Vector3))
        {
            Vector3 v = (Vector3)value;
            return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
        }

        // Vector2: compact format
        if (type == typeof(Vector2))
        {
            Vector2 v = (Vector2)value;
            return $"({v.x:F2}, {v.y:F2})";
        }

        if (type == typeof(Color))
        {
            Color c = (Color)value;
            return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
        }

        // TODO Quaternion
        // TODO Enum

        // Default: use ToString()
        return value.ToString();
    }
}