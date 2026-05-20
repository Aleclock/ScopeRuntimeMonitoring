# Step 5 Guide: Box Anchors, Auto-Stacking, and Cleaner Box Placement

This guide keeps the copy-paste style and focuses on the layout problem you found.

Goal
----
Let each box declare **where it wants to live** on screen:

- `TopLeft`
- `TopRight`
- `BottomLeft`
- `BottomRight`

Then add a **placement step** that decides the final screen position.

That means:

- `MonitorBoxDefinition` says the box’s intent.
- A placement manager groups boxes by anchor.
- Boxes using the same anchor are stacked one after another.
- The title area and row area no longer fight for the same vertical space.

What This Solves
----------------
This guide addresses the two issues you found:

1. The row area begins too high and gets trimmed.
2. Multiple boxes using the same anchor overlap instead of stacking.

The important idea is:

- **definition** = what the box wants
- **placement** = where the box actually goes

Recommended Architecture
------------------------
Keep these responsibilities separate:

- `MonitorBoxDefinition` → size, title, colors, anchor, spacing
- `MonitorBoxView` → render one box
- `MonitorPanelController` → build all boxes and pass them to the placer
- `BoxPlacementManager` → group and stack boxes by anchor

Step 1: Keep the Definition Pure
---------------------------------
Your `MonitorBoxDefinition` should only describe the box.

It should contain data like:

- `Id`
- `Title`
- `PanelSize`
- `Margin`
- `RowHeight`
- `RowSpacing`
- `TitleHeight`
- `Anchor`
- colors
- padding

It should **not** decide its final screen position.

Why this matters:

- definitions stay simple
- placement logic can evolve without changing the data model
- duplicate anchors become easy to manage

Step 2: Add an Anchor Field
---------------------------
Add a field that says where the box wants to start.

Example:

```csharp
public BoxAnchor Anchor = BoxAnchor.TopLeft;
```

This does **not** mean the box will always be placed there directly.
It only says which group it belongs to.

Step 3: Reserve Space for the Title
-----------------------------------
The title and the scroll area must not overlap.

A simple rule is:

- title gets a fixed height
- scroll area starts below it

Recommended values:

- `TitleHeight = 30f`
- row area starts at `y = -TitleHeight`

This prevents the first row from being clipped by the title area.

If you already have a box size like `430 x 280`, then the usable content area is:

- total panel height
- minus title height
- minus outer padding

Step 4: Introduce a Placement Manager
-------------------------------------
Create a separate place to compute positions.

Use a small manager like:

- `BoxPlacementManager`
- or `MonitorBoxPlacementManager`

Its job is to:

1. group boxes by `Anchor`
2. sort them inside each group
3. assign final positions
4. move the next box down by previous box height + spacing

Why this matters:

- boxes with the same anchor no longer overlap
- each anchor zone becomes its own stack
- adding a new box does not require hand-tuning positions

Step 5: Use a Stack Offset Per Anchor
-------------------------------------
The stacking rule should be simple.

For each anchor group:

- start from the safe-area corner
- place the first box there
- move the next box down by:
  - previous box height
  - plus gap between boxes

Example rule:

```text
nextY = currentY - currentBoxHeight - boxSpacing
```

For bottom anchors, the stacking direction is still vertical, but you subtract/add relative to that anchor so the group stays inside the safe area.

Step 6: Place Boxes After All Definitions Are Known
----------------------------------------------------
Do not place each box immediately in its own constructor.

Instead:

1. create all definitions
2. create all views
3. send the collection to the placement manager
4. let the manager assign positions

This is important because stacking needs to know how many boxes share the same anchor.

Step 7: Example Behavior
------------------------
If you define these boxes:

- `Gameplay` → `TopLeft`
- `Transform` → `TopLeft`
- `Physics` → `BottomRight`

Then the result should be:

- `Gameplay` at top-left
- `Transform` directly below `Gameplay`
- `Physics` at bottom-right

So the final behavior becomes:

- same anchor = stacked list
- different anchor = different corner group

Step 8: Keep Scroll Layout Independent
--------------------------------------
The row clipping issue should be solved inside the box layout, not by the anchor placement.

Box placement decides:

- where the box goes on screen

Box internals decide:

- where the title goes
- where the scroll content begins
- how rows are clipped and sized

Do not mix those two layers.

Step 9: Suggested Box Layout Flow
---------------------------------
Use this mental model:

1. `MonitorPanelController` creates the box definitions.
2. The placer groups them by anchor.
3. The placer computes final anchored positions.
4. `MonitorBoxView` draws the panel.
5. `MonitorRowView` draws each row.

That separation keeps the system easier to extend.

Step 10: Quick Checklist
------------------------
Before you test, make sure:

- boxes have a declared anchor
- title height is reserved
- content starts below the title
- same-anchor boxes get a stack offset
- box placement happens after all boxes are known

If it works, you should see:

- no overlap for two `TopLeft` boxes
- rows fully visible inside the scroll area
- clear separation between box position and box content

Optional Next Step
------------------
If you want, the next guide can be:

- a ScriptableObject-based configuration guide
- or a placement manager guide with concrete copy-paste code

