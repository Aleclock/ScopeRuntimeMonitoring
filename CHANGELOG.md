# Changelog

## [1.1.0] - 2026-07-27
### Added
- Introduced `IMonitorGroupCustomizer` and `IMonitorSubGroupCustomizer` interfaces to customize box/group names dynamically at runtime per-instance.
- Added `Monitor.GroupCustomizer` and `Monitor.SubGroupCustomizer` global delegates to customize group naming rules at runtime.
- Added native vertical bullet-point formatting for `IEnumerable` lists and collections.
- Added support for auto-wrap and dynamic height on value labels, automatically aligning multi-line/list displays to the top-left.
- Added extensible custom widget registry (`Monitor.RegisterCustomWidget`) to allow developers to create custom UI elements for specific variants.

## [1.0.5] - 2026-07-24
### Added
- Added support for specifying custom display order for subgroups using the new `SubGroupOrder` property in `MonitorAttribute`.
- Implemented sorting of subgroups and flat monitored elements within group boxes.

## [1.0.4] - 2026-07-22
### Added
- Supported specifying custom display order in `MonitorAttribute` (e.g., `[Monitor("Label", Order = 1)]`).
- Implemented sorting from top to bottom based on the configured order of the monitored values inside their UI containers.
