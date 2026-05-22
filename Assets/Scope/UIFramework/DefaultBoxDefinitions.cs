using System.Collections.Generic;
using UnityEngine;

public static class DefaultBoxDefinitions
{
    public static List<MonitorBoxDefinition> Create()
    {
        var gameplayBox = new MonitorBoxDefinition
        {
            Id = "gameplay",
            Title = "Gameplay",
            FilterRule = new NameContainsFilterRule(""),
            SortRule = new NameAscendingSortRule()
        };


        return new List<MonitorBoxDefinition>
        {
            gameplayBox
        };
    }
}