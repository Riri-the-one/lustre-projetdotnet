# Third-party Assets

The product photographs and original styling were already in this repository. They were retained; no ownership or redistribution licence is inferred. The isolated demo references the existing cleanser, moisturizer, serum and sunscreen photographs. Review their rights before distributing them beyond a personal portfolio.

Bundled browser dependencies are recorded in `scripts/vendor/package-lock.json`: jQuery 3.7.1, jQuery Validation 1.21.0, jQuery Unobtrusive Validation 4.0.0 and Bootstrap 5.3.8. Original licence notices are included under `wwwroot/lib/`. Restore with `npm ci --prefix scripts/vendor`, then `node scripts/sync-vendor.cjs`; audit with `npm audit --prefix scripts/vendor`. The pre-existing Bootstrap CSS/theme is retained.

Playwright is locked separately in `tests/browser/package-lock.json`. The image codec packages StbImageSharp and StbImageWriteSharp are managed NuGet dependencies; consult their package metadata and upstream public-domain/MIT notices. No Six Labors package is included in the final dependency graph.

External Google Fonts and Bootstrap Icons are loaded by the original layouts; the site falls back to local fonts if unavailable. The new missing-product SVG is repository-native artwork.
