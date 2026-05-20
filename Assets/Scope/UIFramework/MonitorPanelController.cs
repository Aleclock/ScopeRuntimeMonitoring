using System.Collections.Generic;

public class MonitorPanelController : DebugPanelBase
{
    private readonly List<MonitorBoxView> _boxViews = new List<MonitorBoxView>();
    private List<MonitorBoxDefinition> _definitions;

    public override void Initialize()
    {
        _definitions = DefaultBoxDefinitions.Create();
        var allHandles = new List<IMonitorHandle>(Monitor.Registry.GetMonitorHandles());
        var placementManager = new BoxPlacementManager();
        var handlesByBoxId = new Dictionary<string, IReadOnlyList<IMonitorHandle>>();

        foreach (var definition in _definitions)
        {
            var handlesForBox = BuildHandleListForBox(allHandles, definition);
            handlesByBoxId[definition.Id] = handlesForBox;
            definition.PanelSize = placementManager.CalculatePanelSize(definition, handlesForBox);
        }

        var positions = placementManager.BuildPositions(_definitions);

        foreach (var definition in _definitions)
        {
            var boxView = new MonitorBoxView(transform, definition, handlesByBoxId[definition.Id]);

            if (positions.TryGetValue(definition.Id, out var position))
            {
                boxView.SetPosition(position);
            }

            _boxViews.Add(boxView);
        }
    }

    private IReadOnlyList<IMonitorHandle> BuildHandleListForBox(List<IMonitorHandle> source, MonitorBoxDefinition definition)
    {
        var filtered = new List<IMonitorHandle>();

        foreach (var handle in source)
        {
            if (definition.FilterRule == null || definition.FilterRule.Include(handle))
            {
                filtered.Add(handle);
            }
        }

        if (definition.SortRule != null)
        {
            filtered.Sort(definition.SortRule.Compare);
        }

        return filtered;
    }

    private void Update()
    {
        foreach (var boxView in _boxViews)
            boxView.RefreshRows();
    }

    public override void InizializePanelData()
    {
        
    }

    public override void UpdatePanelData()
    {
        
    }
}