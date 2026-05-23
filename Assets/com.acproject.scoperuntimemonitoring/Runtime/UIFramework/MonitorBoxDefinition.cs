using UnityEngine;

[System.Serializable]
public sealed class MonitorBoxDefinition
{
    public string Id;
    public string Title;

    public IBoxFilterRule FilterRule;
    public IBoxSortRule SortRule;

    public MonitorBoxDefinition()
    {
    }
}