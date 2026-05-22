# Step 15 — Simplify `MonitorBoxDefinition` So It Only Describes Grouping

Goal
----
Remove the layout/sizing duplication from `MonitorBoxDefinition` and keep only the data that actually describes *what belongs in a box*.

This is the cleanup you asked for:
- `MonitorPanelSettings` keeps global UIToolkit defaults.
- `MonitorPanelOverrides` keeps per-panel overrides.
- `MonitorBoxOverrides` keeps per-box overrides.
- `MonitorBoxDefinition` becomes the small grouping model that tells the builder which monitors belong together.

Why this cleanup is needed
--------------------------
Right now `MonitorBoxDefinition` is carrying both:
- content/grouping information, and
- layout/sizing/style information.

That made sense in the older uGUI path, but for the UIToolkit path it is mostly duplicated because the same layout information already exists in:
- `MonitorPanelSettings`
- `MonitorPanelOverrides`
- `MonitorBoxOverrides`

If those other objects already define the layout behavior, then `MonitorBoxDefinition` should not repeat it.

What `MonitorBoxDefinition` should keep
--------------------------------------
For a UIToolkit-first design, `MonitorBoxDefinition` should usually keep only:
- `Id`
- `Title`
- `FilterRule`
- `SortRule`

That is enough to tell the builder:
- which handles belong in the box,
- what the box is called,
- how to sort the rows.

What should move elsewhere
--------------------------
These fields are layout-specific and should live in settings/overrides instead of `MonitorBoxDefinition`:
- `Anchor`
- `PanelSize`
- `Margin`
- `AutoSizeWidth`
- `AutoSizeHeight`
- `MinPanelWidth`
- `MaxPanelWidth`
- `MinPanelHeight`
- `MaxPanelHeight`
- `EstimatedCharacterWidth`
- `FontSize`
- `TitleHeight`
- `RowHeight`
- `RowSpacing`
- `LayoutRule`
- `PanelColor`
- `RowColor`
- `TextColor`
- `TitleColor`
- `PaddingLeft`
- `PaddingRight`
- `PaddingTop`
- `PaddingBottom`

If you still need some of those values, the clean place for them is:
- global defaults in `MonitorPanelSettings`
- per-panel overrides in `MonitorPanelOverrides`
- per-box overrides in `MonitorBoxOverrides`

Files involved
--------------
- [Assets/Scope/UIFramework/MonitorBoxDefinition.cs](Assets/Scope/UIFramework/MonitorBoxDefinition.cs)
- [Assets/Scope/UIFramework/DefaultBoxDefinitions.cs](Assets/Scope/UIFramework/DefaultBoxDefinitions.cs)
- [Assets/Scope/Settings/MonitorPanelSettings.cs](Assets/Scope/Settings/MonitorPanelSettings.cs)
- [Assets/Scope/Settings/MonitorPanelOverrides.cs](Assets/Scope/Settings/MonitorPanelOverrides.cs)
- [Assets/Scope/Settings/MonitorBoxOverrides.cs](Assets/Scope/Settings/MonitorBoxOverrides.cs)
- [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs)

Step-by-step edits
------------------

1) Reduce `MonitorBoxDefinition` to grouping-only data

Open [Assets/Scope/UIFramework/MonitorBoxDefinition.cs](Assets/Scope/UIFramework/MonitorBoxDefinition.cs).

Replace the current class with this simplified version:

```csharp
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
```

This removes all layout/sizing duplication from the definition object.

2) Update `DefaultBoxDefinitions.Create()` to only set grouping data

Open [Assets/Scope/UIFramework/DefaultBoxDefinitions.cs](Assets/Scope/UIFramework/DefaultBoxDefinitions.cs).

Replace the current `gameplayBox` initialization with this slimmer version:

```csharp
        var gameplayBox = new MonitorBoxDefinition
        {
            Id = "gameplay",
            Title = "Gameplay",
            FilterRule = new NameContainsFilterRule(""),
            SortRule = new NameAscendingSortRule()
        };
```

Then keep the return list as one box only:

```csharp
        return new List<MonitorBoxDefinition>
        {
            gameplayBox
        };
```

If you no longer need the `CreateBox(int i)` helper, remove it too.

3) Move layout values into `MonitorPanelSettings` if they are global

If you still want global UIToolkit defaults for size and spacing, add them to [Assets/Scope/Settings/MonitorPanelSettings.cs](Assets/Scope/Settings/MonitorPanelSettings.cs).

Example block you can add under `Runtime Defaults` or a new section such as `UIToolkit Layout Defaults`:

```csharp
    [Header("UIToolkit Layout Defaults")]
    [Min(0f)] public float defaultPanelWidth = 430f;
    [Min(0f)] public float defaultPanelHeight = 280f;
    [Min(0f)] public float defaultMargin = 12f;
    [Min(0f)] public float defaultFontSize = 17f;
    [Min(0f)] public float defaultTitleHeight = 30f;
    [Min(0f)] public float defaultRowHeight = 26f;
    [Min(0f)] public float defaultRowSpacing = 4f;
```

Keep only the values you actually use in the UIToolkit builder.

4) Let `MonitorPanelOverrides` override those defaults per panel

Open [Assets/Scope/Settings/MonitorPanelOverrides.cs](Assets/Scope/Settings/MonitorPanelOverrides.cs).

If you want the panel to override the same layout values, add matching fields and override switches here as well.

Example pattern:

```csharp
    [Header("Override Switches")]
    [SerializeField] private bool overridePanelWidth;
    [SerializeField] private bool overridePanelHeight;
    [SerializeField] private bool overrideMargin;
    [SerializeField] private bool overrideFontSize;
    [SerializeField] private bool overrideTitleHeight;
    [SerializeField] private bool overrideRowHeight;
    [SerializeField] private bool overrideRowSpacing;

    [Header("Per-Panel Values")]
    [SerializeField, Min(0f)] private float panelWidth = 430f;
    [SerializeField, Min(0f)] private float panelHeight = 280f;
    [SerializeField, Min(0f)] private float margin = 12f;
    [SerializeField, Min(0f)] private float fontSize = 17f;
    [SerializeField, Min(0f)] private float titleHeight = 30f;
    [SerializeField, Min(0f)] private float rowHeight = 26f;
    [SerializeField, Min(0f)] private float rowSpacing = 4f;
```

If you already have a different override structure, keep the structure, but make sure the layout values live here rather than in `MonitorBoxDefinition`.

5) Optionally add per-box layout overrides in `MonitorBoxOverrides`

If one box needs a special size or spacing, use [Assets/Scope/Settings/MonitorBoxOverrides.cs](Assets/Scope/Settings/MonitorBoxOverrides.cs).

Example fields to add:

```csharp
    [Header("Layout Overrides")]
    [SerializeField] private bool overridePanelWidth = false;
    [SerializeField, Min(0f)] private float panelWidth = 430f;

    [SerializeField] private bool overridePanelHeight = false;
    [SerializeField, Min(0f)] private float panelHeight = 280f;

    [SerializeField] private bool overrideFontSize = false;
    [SerializeField, Min(0f)] private float fontSize = 17f;

    [SerializeField] private bool overrideRowHeight = false;
    [SerializeField, Min(0f)] private float rowHeight = 26f;

    [SerializeField] private bool overrideRowSpacing = false;
    [SerializeField, Min(0f)] private float rowSpacing = 4f;
```

That keeps per-box exceptions local without bloating the shared definition model.

6) Make `DynamicColumnLayout` read layout from settings/overrides instead of `MonitorBoxDefinition`

Open [Assets/Scope/DynamicColumnLayout.cs](Assets/Scope/DynamicColumnLayout.cs).

Where you build the UIToolkit box, stop reading layout data from `MonitorBoxDefinition` fields that no longer exist.

Instead, read from the resolved config or box overrides.

A clean pattern is:
- global defaults from `MonitorPanelSettings`
- per-panel overrides from `MonitorPanelOverrides`
- per-box overrides from `MonitorBoxOverrides`
- grouping and label text from `MonitorBoxDefinition`

That means `DynamicColumnLayout` should use `definition.Title`, `definition.FilterRule`, and `definition.SortRule` for grouping, while using the resolved layout settings for size, spacing, and style.

Example usage pattern inside the builder:

```csharp
        var panelWidth = resolvedConfig.PanelSettings.defaultPanelWidth;
        var panelHeight = resolvedConfig.PanelSettings.defaultPanelHeight;
        var margin = resolvedConfig.PanelSettings.defaultMargin;
        var fontSize = resolvedConfig.PanelSettings.defaultFontSize;
        var titleHeight = resolvedConfig.PanelSettings.defaultTitleHeight;
        var rowHeight = resolvedConfig.PanelSettings.defaultRowHeight;
        var rowSpacing = resolvedConfig.PanelSettings.defaultRowSpacing;
```

Then override those values if `MonitorPanelOverrides` or `MonitorBoxOverrides` say to.

What the final design should feel like
-------------------------------------
After this cleanup, the architecture becomes simpler:

- `MonitorBoxDefinition` answers: "Which values belong together?"
- `MonitorPanelSettings` answers: "What are the default UIToolkit layout values?"
- `MonitorPanelOverrides` answers: "What should this panel change?"
- `MonitorBoxOverrides` answers: "What should this single box change?"
- `DynamicColumnLayout` answers: "How do I build the UIToolkit UI from those inputs?"

That is the clean split you were looking for.

Testing checklist
-----------------
1. Simplify `MonitorBoxDefinition` first.
2. Update `DefaultBoxDefinitions.Create()`.
3. Make sure `DynamicColumnLayout` still compiles.
4. Enter Play mode.
5. Verify the box still appears and displays the monitored values.
6. Verify the layout still looks right after moving layout values into settings/overrides.

Important warning
-----------------
Do not delete the layout fields from `MonitorBoxDefinition` until the UIToolkit builder no longer uses them.
If you remove them too early, the project will not compile.

Recommended order
-----------------
1. Move the values into settings/overrides.
2. Update `DynamicColumnLayout` to read the new source.
3. Remove the old fields from `MonitorBoxDefinition`.
4. Simplify `DefaultBoxDefinitions.Create()`.

If you want, I can make the next guide a concrete "migration order" document that tells you exactly which file to edit first, second, third, and fourth so you do not break compilation while moving the fields.
