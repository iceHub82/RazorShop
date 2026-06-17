# Editorial Bold Restyle — Design Spec

**Date:** 2026-06-17
**Branch:** `feature/editorial-bold-restyle`
**Status:** Approved (brainstorming)

## Goal

Give the public storefront a visual makeover in an "Editorial Bold" direction —
black/white type with a single electric orange-red accent — without changing any
page layout or backend behavior. A pure styling layer over the existing markup.

## Scope

In scope (public storefront only):

- Global chrome: navigation (`Slices/Navigation.cshtml`), footer (`Slices/Footer.cshtml`), shared layouts.
- Pages: Home, Products, Product, Cart, Checkout, and the info pages (About, Terms,
  DataPolicy, CustomerService, PayAndDelivery, OrderSuccess, OrderFailure).
- Buttons, forms/inputs, links, product grid/cards, headings/hero.

Non-goals:

- Admin area (`Pages/Admin/**`, `_AdminLayout.cshtml`) — untouched.
- Admin-driven design switching / a settings store (was discussed as "Phase 2", explicitly dropped).
- Dark-mode rework (`color-modes.js` stays as-is).
- New page layouts, new components, copy/content changes, custom webfonts.

## Mechanism (theme attaches additively)

The new look is applied through a `data-theme="editorial"` attribute on the
`<html>` element and a dedicated stylesheet whose every rule is scoped under
`[data-theme="editorial"]`.

- When the attribute is absent, **no** editorial rule applies → the classic
  Bootstrap look is byte-for-byte unchanged. This keeps the change low-risk and
  makes rollback a one-line edit, and leaves the door open for a future admin toggle.
- Phase 1 hardcodes `data-theme="editorial"` on the public layouts. There is no
  toggle UI in this phase.

### Files

New:

- `wwwroot/css/theme-editorial.css` — all editorial rules, scoped under `[data-theme="editorial"]`.

Edited:

- `Pages/Shared/_Layout.cshtml` — add `data-theme="editorial"` to `<html>`; add a
  cache-busted `<link>` to `theme-editorial.css` (same `File.GetLastWriteTime(...).Ticks`
  pattern already used for `site.css`).
- `Pages/Shared/_CheckoutLayout.cshtml` — same two edits.

No `.cshtml` structural markup changes — styling is achieved by overriding existing
Bootstrap classes/components from CSS.

## Visual specification

### Tokens (CSS custom properties, set on `[data-theme="editorial"]`)

| Token | Value | Use |
|-------|-------|-----|
| `--ink` | `#111111` | base text, nav/footer background |
| `--paper` | `#ffffff` | page background |
| `--accent` | `#ff4d2e` | button/fill, active "Kurv", hover, focus rings, accent blocks |
| `--accent-text` | `#d6381c` | accent-coloured **text on white** (links) — darker for AA contrast |

Bootstrap variable overrides scoped to the theme: `--bs-primary` → `--accent`,
body font family → system stack, plus targeted component rules below.

### Typography

- System font stack only (no webfont, no new files):
  `system-ui, -apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif`.
- Headings (`h1`–`h3`), hero, navbar brand, nav links: heaviest system weight
  (700–800), `text-transform: uppercase`, tight `letter-spacing` (≈ `-0.01em` on
  large headings; `+0.08em` on small uppercase nav/labels), tight `line-height`.
- Body text: system stack, normal weight/case.

### Components

- **Navigation:** black (`--ink`) bar, white text; nav links uppercase, letter-spaced;
  "Kurv"/cart and link hover in `--accent`. Override the existing `navbar-light bg-light`
  via theme-scoped rules (no markup edit).
- **Footer:** black background to bookend the nav; light text; links in white, hover `--accent`.
- **Buttons:** squared corners (`border-radius: 2px`), bold, uppercase, slight letter-spacing.
  Primary = `--accent` fill with white text. Secondary = outlined ink.
- **Links (body):** `--accent-text` colour, underline on hover.
- **Product grid/cards:** tighter spacing, square corners, subtle border; image/title
  hover lifts to `--accent` accenting. No structural change to the grid.
- **Forms/inputs (incl. checkout):** squared inputs, `--accent` focus ring/border on focus.

### Accessibility

- Button text: white on `--accent` (`#ff4d2e`) is used only for **large/bold** button
  labels (≥ 3:1 large-text threshold). Acceptable.
- Link text on white uses `--accent-text` (`#d6381c`, ≥ 4.5:1) — never the brighter fill.
- Focus rings remain visible (do not remove outlines; restyle them in `--accent`).
- Existing keyboard/ARIA behavior from Bootstrap components is preserved (markup unchanged).

## Verification

- It is CSS; no unit tests are added for styling.
- Existing `SmokeTests` already assert public pages return 2xx; adding a `<link>` and an
  attribute will not break them.
- Add **one** assertion to the smoke suite: the home page HTML contains
  `data-theme="editorial"` and a reference to `theme-editorial.css` — a regression guard
  that the theme stays wired into the layout.
- Final acceptance is visual, via running the app (the `run` skill) and eyeballing
  home / product / cart / checkout against the Editorial Bold direction.

## Rollback

Remove `data-theme="editorial"` from the two layouts (or delete the `<link>`). The
classic Bootstrap look returns immediately; `theme-editorial.css` becomes inert.
