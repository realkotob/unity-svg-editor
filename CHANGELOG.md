# Changelog

All notable changes to this project should be documented in this file.

The format is based on Keep a Changelog, and this project is intended to follow Semantic Versioning.

## [Unreleased]

## [1.1.0] - 2026-03-24

### Added

- README path edit demo assets with a new GIF and screenshot section under `Path Edit Mode`
- Regression coverage for `path-edit-regression-suite.svg` `polygon-shape` path edit anchors

### Changed

- Path edit quadratic overlays now display split handles from the moment edit mode opens
- Closed primitive path overlays now preserve all anchors for closed shapes that do not repeat the start point explicitly

## [1.0.0] - 2026-03-20

### Added

- Public release README for package installation, usage, support boundaries, versioning, and release workflow
- Release-history template with changelog structure and git tag guidance

### Changed

- Package version updated to `1.0.0` for the first stable release baseline
- Package description updated to better match the current product scope

### Known Limitations

- Advanced SVG features still depend on Unity `VectorImage` importer behavior and fallback rendering
- Direct `path` editing and direct gradient editing are not currently supported
- `textPath`, per-`tspan` editing, `filter`, `image`, and `style` are not first-class editing targets
