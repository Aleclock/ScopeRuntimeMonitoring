UIToolkit Flexbox Example

Files created:
- FlexContainer.uxml
- FlexboxExample.uss
- FlexboxExampleBootstrap.cs
- Editor/CreateFlexboxUIDocument.cs (Menu item)

How to use:
1. In Unity Editor, open the project and wait for compilation.
2. From the top menu choose: Tools > UIToolkit > Create Flexbox Example
   - This creates a GameObject named `UIToolkit_FlexboxExample` with a `UIDocument`.
3. Select the new GameObject in the Hierarchy. Press Play to preview.

Notes:
- The example now builds the flex panels directly in C# so you can verify wrapping without stylesheet setup.
- `FlexContainer.uxml` and `FlexboxExample.uss` are still kept in the folder for reference, but they are no longer required for the test.
- If you don't see the panel in Play mode, ensure the UI Toolkit packages are installed and that `PanelSettings` asset was created under `Assets/Scope/UIFramework/UIToolkitExample/DefaultPanelSettings.asset`.

Next steps:
- Replace the static panels in the UXML with runtime-generated panels:
  - Create a small script that loads the UIDocument at runtime and clones `VisualTreeAsset` items into `rootVisualElement` using `CloneTree()`.
  - Populate each panel's labels with data from your `Monitor.Registry`.
