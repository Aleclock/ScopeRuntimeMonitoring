# Step 3 Guide: Screen-Based Positioning, Flex Layout, and Scroll

This step replaces the manual line-by-line positioning with a layout system that can:

- anchor the panel to a screen position,
- adapt to safe area and resolution,
- stack rows automatically,
- scroll when the content grows,
- keep customization values in one place.

This is the first step where the panel starts acting like a real UI system instead of a hand-placed list.

## What You Are Building

You will build a panel that has three layers:

1. a screen-anchored panel root,
2. a scroll view viewport,
3. a vertical content container that lays out rows automatically.

That means:

- no more manual `y -= 24` logic,
- rows flow using a layout group,
- the panel height can stay fixed while the content grows,
- scrolling appears automatically when needed.

## What You Should Already Have

This guide assumes:

- `MonitorsPanel` already works,
- `Monitor.Registry.GetMonitorHandles()` returns the handles you want to show,
- `FieldMonitorHandle` and `PropertyMonitorHandle` both work,
- `ValueFormatter.FormatValue(object)` already formats values.

If that is true, this step is safe to apply.

## The Goal Of This Step

By the end of this step you should be able to:

- place the panel in a predictable screen position,
- let the content stack vertically without manual coordinates,
- scroll through the entries when there are too many,
- keep the code easy to customize later.

## Why This Is Better Than Manual Positioning

Manual positioning is fine for 1 or 2 rows, but it becomes fragile quickly.

With layout components:

- the panel can grow without rewriting code,
- spacing stays consistent,
- the same code works with different resolutions,
- adding or removing rows does not require recalculating every `y` value.

## Step 1: Decide The Layout Model

Use this model:

- the panel root is anchored to the screen,
- the inner content is a vertical stack,
- each row takes the full width of the content,
- the content scrolls inside a fixed viewport.

This gives you the closest thing to a flex layout in Unity UI.

## Step 2: Replace Manual Row Placement With A Scroll Layout

You will update `MonitorsPanel` so it creates:

- a root background panel,
- a scroll view,
- a viewport,
- a content container,
- one row per handle.

### Copy-paste panel code

Replace your current `MonitorsPanel.cs` with this version:

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonitorsPanel : DebugPanelBase
{
    [Header("Panel Placement")]
    public Vector2 anchoredPosition = new Vector2(12f, -12f);
    public Vector2 panelDimensions = new Vector2(420f, 320f);

    [Header("Layout")]
    public float rowHeight = 28f;
    public float rowSpacing = 4f;
    public float paddingLeft = 10f;
    public float paddingRight = 10f;
    public float paddingTop = 10f;
    public float paddingBottom = 10f;

    [Header("Text")]
    public int fontSize = 18;
    public Color textColor = Color.white;

    private readonly List<IMonitorHandle> handles = new List<IMonitorHandle>();
    private readonly List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();

    private RectTransform panelRect;
    private RectTransform viewportRect;
    private RectTransform contentRect;
    private ScrollRect scrollRect;
    private VerticalLayoutGroup layoutGroup;
    private ContentSizeFitter contentSizeFitter;

    public override void Initialize()
    {
        handles.Clear();
        texts.Clear();

        handles.AddRange(Monitor.Registry.GetMonitorHandles());

        CreatePanelRoot();
        CreateScrollView();
        CreateRows();
        RefreshPanelSize();
    }

    private void Update()
    {
        for (int i = 0; i < handles.Count; i++)
        {
            if (i >= texts.Count)
            {
                break;
            }

            texts[i].text = $"{handles[i].Name}: {handles[i].GetValueString()}";
        }
    }

    private void CreatePanelRoot()
    {
        panelBackground = new GameObject("PanelBackground").AddComponent<Image>();
        panelBackground.transform.SetParent(transform, false);
        panelBackground.color = new Color(0f, 0f, 0f, 0.55f);

        panelRect = panelBackground.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = panelDimensions;
        panelRect.anchoredPosition = anchoredPosition;

        var border = panelBackground.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(1f, 1f, 1f, 0.08f);
        border.effectDistance = new Vector2(1f, -1f);
    }

    private void CreateScrollView()
    {
        var scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollObject.transform.SetParent(panelBackground.transform, false);

        var scrollImage = scrollObject.GetComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0f);

        scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 30f;

        var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;

        var mask = scrollObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);

        var viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);

        var viewportMask = viewportObject.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);

        contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        layoutGroup = contentObject.GetComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.spacing = rowSpacing;
        layoutGroup.padding = new RectOffset(
            Mathf.RoundToInt(paddingLeft),
            Mathf.RoundToInt(paddingRight),
            Mathf.RoundToInt(paddingTop),
            Mathf.RoundToInt(paddingBottom));

        contentSizeFitter = contentObject.GetComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
    }

    private void CreateRows()
    {
        for (int i = 0; i < handles.Count; i++)
        {
            var row = CreateRow(handles[i], i);
            texts.Add(row);
        }
    }

    private TextMeshProUGUI CreateRow(IMonitorHandle handle, int index)
    {
        var rowObject = new GameObject($"Row_{index}_{handle.Name}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rowObject.transform.SetParent(contentRect, false);

        var rowImage = rowObject.GetComponent<Image>();
        rowImage.color = new Color(1f, 1f, 1f, 0.04f);

        var rowLayout = rowObject.GetComponent<LayoutElement>();
        rowLayout.minHeight = rowHeight;
        rowLayout.preferredHeight = rowHeight;
        rowLayout.flexibleHeight = 0f;

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(rowObject.transform, false);

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = $"{handle.Name}: {handle.GetValueString()}";
        label.fontSize = fontSize;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = false;

        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);

        return label;
    }

    private void RefreshPanelSize()
    {
        panelRect.sizeDelta = panelDimensions;
    }

    public override void InizializePanelData()
    {
    }

    public override void UpdatePanelData()
    {
    }
}
```

## What This Code Does

### 1. Screen-based positioning

The panel root uses:

- `anchorMin = (0, 1)`
- `anchorMax = (0, 1)`
- `pivot = (0, 1)`

That means it is anchored to the top-left corner of the screen.

You can move it by changing `anchoredPosition`.

### 2. Scroll layout

The `ScrollRect` keeps the visible area fixed.

The `Content` object grows vertically as rows are added.

When content exceeds the viewport, scrolling kicks in automatically.

### 3. Flex-style stacking

The `VerticalLayoutGroup` acts like a vertical flex column.

Each row gets:

- a consistent height,
- full width,
- consistent spacing.

### 4. Row customization

Each row is a real UI object, so later you can easily add:

- icons,
- colors,
- collapse/expand,
- per-row padding,
- buttons.

## Step 3: Keep The Panel Responsive

The panel root already uses screen anchors.

If you want safer positioning across devices, keep these rules:

- use top-left anchoring for a debug panel,
- keep a fixed width and height for the viewport,
- let the content scroll instead of expanding the panel forever.

If you want to move the panel later, change only `anchoredPosition`.

## Step 4: Optional Screen Safe Area Adjustment

If you want to account for notches or cutouts, add this helper to `MonitorsPanel` and call it from `CreatePanelRoot()` after setting `panelRect`.

```csharp
private void ApplySafeAreaOffset()
{
    var safeArea = Screen.safeArea;
    var screenWidth = Screen.width;
    var screenHeight = Screen.height;

    var leftOffset = safeArea.xMin;
    var topOffset = screenHeight - safeArea.yMax;

    panelRect.anchoredPosition = new Vector2(
        anchoredPosition.x + leftOffset,
        anchoredPosition.y - topOffset);
}
```

Use it like this:

```csharp
CreatePanelRoot();
ApplySafeAreaOffset();
CreateScrollView();
CreateRows();
```

This keeps the panel away from unsafe screen edges.

## Step 5: How To Customize The Layout

You now have simple knobs to tune:

- `panelDimensions` controls the outer size,
- `anchoredPosition` controls where the panel sits,
- `rowHeight` controls the height of each line,
- `rowSpacing` controls the gap between rows,
- `paddingLeft`, `paddingRight`, `paddingTop`, `paddingBottom` control inner spacing,
- `fontSize` controls readability.

That is the beginning of your high-customization layer.

## Step 6: Test The Result

To verify the step:

1. Enter Play Mode.
2. Add enough monitored values so the list exceeds the panel height.
3. Confirm the panel stays fixed in place.
4. Scroll inside the panel.
5. Confirm rows remain aligned and readable.

If you see all of these, the step is done.

## Step 7: What You Learned

This step separates responsibilities better:

- the runtime discovers values,
- the formatter formats them,
- the panel controls layout,
- the scroll view handles overflow.

That separation is what will let you do the more advanced customization next.

## Step 8: What Comes Next

The natural next milestone after this is:

- a more flexible layout system,
- per-row customization,
- grouping and filtering,
- then visual polish.

If you want to continue in order, Step 4 should be a guide for high-customization of the layout and box styling, not more runtime work.
