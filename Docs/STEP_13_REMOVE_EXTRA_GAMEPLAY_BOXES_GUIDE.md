# Step 13 — Remove the Extra Gameplay1..4 Boxes

Goal
----
Make the runtime UI show only one `Gameplay` box instead of five nearly identical boxes.

Why you are seeing five boxes
-----------------------------
The box count does not come from the number of monitored scripts in the scene.
It comes from `DefaultBoxDefinitions.Create()`.

Right now that method returns these definitions:
- `gameplay`
- `gameplay1`
- `gameplay2`
- `gameplay3`
- `gameplay4`

And each one currently uses a very permissive filter, so they all match the same monitored data. That is why one monitored object can still produce five boxes.

What you want instead
--------------------
If your goal is "one monitored group, one box", then `DefaultBoxDefinitions.Create()` should return only one definition.

This guide shows the exact edits to make.

Files to edit
-------------
- [Assets/Scope/UIFramework/DefaultBoxDefinitions.cs](Assets/Scope/UIFramework/DefaultBoxDefinitions.cs)
- Optional reference: [Assets/Scope/UIFramework/MonitorPanelController.cs](Assets/Scope/UIFramework/MonitorPanelController.cs)

Step-by-step edits
------------------

1) Open `DefaultBoxDefinitions.cs`

Open [Assets/Scope/UIFramework/DefaultBoxDefinitions.cs](Assets/Scope/UIFramework/DefaultBoxDefinitions.cs).

You will see a `Create()` method that builds several `MonitorBoxDefinition` objects.

2) Remove the extra `Gameplay1..4` variables

Delete these temporary definitions:

```csharp
        var gameplayBox1 = CreateBox(1);
        var gameplayBox2 = CreateBox(2);
        var gameplayBox3 = CreateBox(3);
        var gameplayBox4 = CreateBox(4);
        var gameplayBox5 = CreateBox(5);
        var gameplayBox6 = CreateBox(6);
        var gameplayBox7 = CreateBox(7);
        var gameplayBox8 = CreateBox(8);
        var gameplayBox9 = CreateBox(9);
        var gameplayBox10 = CreateBox(10);
```

3) Replace the return list with one definition only

Replace the current `return new List<MonitorBoxDefinition>` block with this exact version:

```csharp
        return new List<MonitorBoxDefinition>
        {
            gameplayBox
        };
```

4) Remove the extra commented entries too

If your current return list includes commented out items like this:

```csharp
            /*
            gameplayBox5,
            gameplayBox6,
            gameplayBox7,
            gameplayBox8,
            gameplayBox9,
            gameplayBox10
            */
```

delete them as well. They are no longer needed if you want only one box.

5) Keep `CreateBox(int i)` only if you still plan to use it later

The `CreateBox(int i)` helper is only useful if you want more boxes in the future. If you are sure you only want one `Gameplay` box, you can keep the method unused for now, or remove it later once you confirm nothing else calls it.

Recommended final `Create()` method
-----------------------------------
Here is the exact final shape you want if the goal is one box only:

```csharp
public static List<MonitorBoxDefinition> Create()
{
    var gameplayBox = new MonitorBoxDefinition
    {
        Id = "gameplay",
        Title = "Gameplay",
        Anchor = BoxAnchor.BottomRight,
        PanelSize = new Vector2(430f, 280f),
        Margin = new Vector2(12f, 12f),
        AutoSizeWidth = true,
        AutoSizeHeight = true,
        MinPanelWidth = 280f,
        MaxPanelWidth = 460f,
        MinPanelHeight = 160f,
        MaxPanelHeight = 340f,
        EstimatedCharacterWidth = 8.5f,
        FontSize = 17,
        TitleHeight = 30f,
        RowHeight = 26f,
        RowSpacing = 4f,
        FilterRule = new NameContainsFilterRule(""),
        SortRule = new NameAscendingSortRule(),
        LayoutRule = new ScreenAnchorLayoutRule(BoxAnchor.BottomRight)
    };

    return new List<MonitorBoxDefinition>
    {
        gameplayBox
    };
}
```

What this changes at runtime
----------------------------
- Only one definition exists.
- Only one `Gameplay` box is built.
- The scene will no longer show `Gameplay1`, `Gameplay2`, `Gameplay3`, or `Gameplay4`.

Important note about data grouping
----------------------------------
Removing the extra definitions only reduces the number of boxes.
It does **not** by itself change how handles are grouped.

If your filter is still too broad, one box may still contain many values. If you want different boxes to show different monitor groups, you need the definitions to use meaningful filters. That is explained below.

How grouping by monitor name works
----------------------------------
This is the part that was confusing, so here is the simple explanation.

A `MonitorBoxDefinition` is not a box itself.
It is a rule that says:
- which monitored handles belong together,
- what the box should be called,
- how the box should be laid out.

In code, the builder does something like this:

```csharp
var handlesForBox = BuildHandleListForBox(allHandles, definition);
```

That means:
- `allHandles` = every registered monitored field/property in the registry,
- `definition` = the rule for one box,
- `handlesForBox` = only the handles that match that rule.

If the rule is too broad, many handles end up in the same box.
If you have multiple definitions with the same broad rule, you get multiple boxes with the same content.

If you want one box per monitor group, the definition must filter by name or id.

Example idea:
- a box named `Gameplay` might include only handles whose names contain `Health`, `Score`, or `Position`.
- another box named `UI` might include only handles whose names contain `Menu`, `Panel`, or `Button`.

So “group by monitor name” means:
- use the monitor member names or ids to decide which handles go into which box,
- instead of letting every box include everything.

Testing checklist
-----------------
1. Open `DefaultBoxDefinitions.cs`.
2. Reduce the returned definitions to one.
3. Enter Play mode.
4. Verify only one `Gameplay` box appears.
5. Verify the box still contains the expected monitored values.

If you still see multiple boxes
-------------------------------
If multiple boxes still appear after this change, then one of these is happening:
- `DefaultBoxDefinitions.Create()` is still returning multiple definitions somewhere else.
- the UI builder is reading a different list of definitions.
- the scene is instantiating another panel/controller in addition to the one you edited.

Next step
---------
If you want, the next guide can show how to make the box definitions use meaningful name-based filters so each box gets different data instead of the same data repeated.
