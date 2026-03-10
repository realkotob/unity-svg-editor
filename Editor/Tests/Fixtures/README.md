# SVG Viewport Fixtures

- `preserve-aspect-none-stretch.svg`
  - `preserveAspectRatio="none"` case. Circle should appear horizontally stretched.
- `preserve-aspect-meet-center.svg`
  - default `meet` case. Circle should stay circular and centered.
- `preserve-aspect-slice-top-right.svg`
  - `preserveAspectRatio="xMaxYMin slice"` case. Content should crop from the top-right alignment.
- `no-viewbox-basic.svg`
  - width/height only, no `viewBox`.
- `negative-coordinates.svg`
  - content extends into negative coordinates.
- `transformed-parent.svg`
  - nested group transform case for move/resize validation.
