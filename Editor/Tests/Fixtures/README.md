# SVG Viewport Fixtures

- `preserve-aspect-none-stretch.svg`
  - `preserveAspectRatio="none"` case. Circle should appear horizontally stretched.
- `preserve-aspect-meet-center.svg`
  - default `meet` case. Circle should stay circular and centered.
- `no-viewbox-basic.svg`
  - width/height only, no `viewBox`.
- `negative-coordinates.svg`
  - content extends into negative coordinates.
- `transformed-parent.svg`
  - nested group transform case for move/resize validation.
