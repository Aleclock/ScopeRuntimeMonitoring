# Changelog

## [1.0.5] - 2026-07-24
### Added
- Added support for specifying custom display order for subgroups using the new `SubGroupOrder` property in `MonitorAttribute`.
- Implemented sorting of subgroups and flat monitored elements within group boxes.

## [1.0.4] - 2026-07-22
### Added
- Supported specifying custom display order in `MonitorAttribute` (e.g., `[Monitor("Label", Order = 1)]`).
- Implemented sorting from top to bottom based on the configured order of the monitored values inside their UI containers.
