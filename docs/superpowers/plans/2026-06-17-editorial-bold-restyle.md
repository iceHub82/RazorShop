# Editorial Bold Restyle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reskin the public storefront in an "Editorial Bold" style (black/white + `#ff4d2e` accent, system fonts) without changing any page layout or backend.

**Architecture:** All new styling lives in one stylesheet, `wwwroot/css/theme-editorial.css`, with every rule scoped under `[data-theme="editorial"]`. The two public layouts set `data-theme="editorial"` on `<html>` and link the stylesheet. When the attribute is absent the classic Bootstrap look is unchanged, so the theme is additive and reverts in one line. No `.cshtml` structural markup changes — existing Bootstrap classes are overridden from CSS.

**Tech Stack:** ASP.NET Core (.NET 10) Razor Slices, Bootstrap 5.3 (CSS custom properties / component variables), xUnit + `WebApplicationFactory` for the one regression test.

**Spec:** `docs/superpowers/specs/2026-06-17-editorial-bold-restyle-design.md`

**Note on verification:** Only Task 1 has an automated test (a regression guard that the theme stays wired). Tasks 2–5 are pure CSS and are verified by `dotnet build` (nothing breaks) plus a visual check in the running app — there is no meaningful unit test for appearance. Do not invent tests for CSS.

---

### Task 1: Wire the theme into both public layouts + base tokens

**Files:**
- Create: `RazorShop.Web/wwwroot/css/theme-editorial.css`
- Modify: `RazorShop.Web/Pages/Shared/_Layout.cshtml` (line 5 `<html>`, after line 13 stylesheet link)
- Modify: `RazorShop.Web/Pages/Shared/_CheckoutLayout.cshtml` (line 4 `<html>`, after line 11 stylesheet link)
- Test: `RazorShop.Tests/SmokeTests.cs`

- [ ] **Step 1: Write the failing test**

Add this method inside the `SmokeTests` class in `RazorShop.Tests/SmokeTests.cs`, immediately after the `Get_endpoints_return_success` method:

```csharp
    [Fact]
    public async Task Home_is_wired_to_the_editorial_theme()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("data-theme=\"editorial\"", html);
        Assert.Contains("theme-editorial.css", html);
    }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test RazorShop.Tests/RazorShop.Tests.csproj --filter Home_is_wired_to_the_editorial_theme`
Expected: FAIL — the assertions don't find `data-theme="editorial"` or `theme-editorial.css` in the home HTML yet.

- [ ] **Step 3: Create the base stylesheet**

Create `RazorShop.Web/wwwroot/css/theme-editorial.css` with exactly:

```css
/* Editorial Bold theme.
   Every rule is scoped under [data-theme="editorial"] so the classic Bootstrap
   look is untouched when the attribute is absent. Public storefront only. */

[data-theme="editorial"] {
    --ink: #111111;
    --paper: #ffffff;
    --accent: #ff4d2e;
    --accent-text: #d6381c; /* darker accent for link text — AA contrast on white */

    /* Scoped Bootstrap overrides */
    --bs-body-bg: var(--paper);
    --bs-body-color: var(--ink);
    --bs-body-font-family: system-ui, -apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    --bs-border-radius: 2px;
    --bs-primary: var(--accent);
    --bs-primary-rgb: 255, 77, 46;
    --bs-link-color-rgb: 214, 56, 28;       /* #d6381c */
    --bs-link-hover-color-rgb: 17, 17, 17;  /* #111 */
}
```

- [ ] **Step 4: Add the attribute and link to `_Layout.cshtml`**

In `RazorShop.Web/Pages/Shared/_Layout.cshtml`, change line 5 from:

```html
<html lang="da">
```
to:
```html
<html lang="da" data-theme="editorial">
```

Then, immediately after the `site.css` link (line 13), add:

```html
    <link rel="stylesheet" href="@($"/css/theme-editorial.css?v={File.GetLastWriteTime(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/css/theme-editorial.css")).Ticks}")" />
```

- [ ] **Step 5: Add the attribute and link to `_CheckoutLayout.cshtml`**

In `RazorShop.Web/Pages/Shared/_CheckoutLayout.cshtml`, change line 4 from:

```html
<html lang="da">
```
to:
```html
<html lang="da" data-theme="editorial">
```

Then, immediately after the `site.css` link (line 11), add:

```html
    <link rel="stylesheet" href="/css/theme-editorial.css" asp-append-version="true" />
```

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test RazorShop.Tests/RazorShop.Tests.csproj --filter Home_is_wired_to_the_editorial_theme`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add RazorShop.Web/wwwroot/css/theme-editorial.css RazorShop.Web/Pages/Shared/_Layout.cshtml RazorShop.Web/Pages/Shared/_CheckoutLayout.cshtml RazorShop.Tests/SmokeTests.cs
git commit -m "Wire editorial theme into public layouts with base tokens

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Black navigation + footer chrome

**Files:**
- Modify: `RazorShop.Web/wwwroot/css/theme-editorial.css` (append)

- [ ] **Step 1: Append the nav + footer rules**

Append to `RazorShop.Web/wwwroot/css/theme-editorial.css`:

```css
/* --- Navigation: black bar, white uppercase links --- */
[data-theme="editorial"] .navbar {
    --bs-navbar-color: rgba(255, 255, 255, .85);
    --bs-navbar-hover-color: var(--accent);
    --bs-navbar-active-color: #fff;
    background-color: var(--ink) !important; /* overrides bg-light */
    border-bottom: none !important;
}

[data-theme="editorial"] .navbar .nav-link {
    text-transform: uppercase;
    letter-spacing: .08em;
    font-weight: 600;
    font-size: .85rem;
}

[data-theme="editorial"] .navbar-brand {
    color: #fff !important;
}

[data-theme="editorial"] .navbar-toggler {
    border-color: rgba(255, 255, 255, .3);
}

[data-theme="editorial"] .navbar-toggler-icon {
    filter: invert(1); /* dark Bootstrap icon -> white */
}

[data-theme="editorial"] .navbar .dropdown-menu {
    --bs-dropdown-link-active-bg: var(--accent);
    border-radius: 2px;
}

/* --- Footer: bookends the black nav --- */
[data-theme="editorial"] #footer {
    background-color: var(--ink);
}

[data-theme="editorial"] #footer,
[data-theme="editorial"] #footer .text-body-secondary,
[data-theme="editorial"] #footer .nav-link {
    color: rgba(255, 255, 255, .8) !important;
}

[data-theme="editorial"] #footer h5 {
    color: #fff;
    text-transform: uppercase;
    letter-spacing: .06em;
    font-weight: 700;
}

[data-theme="editorial"] #footer a:hover {
    color: var(--accent) !important;
}

[data-theme="editorial"] #footer .link-body-emphasis {
    color: #fff !important;
}

[data-theme="editorial"] #footer .border-top {
    border-color: rgba(255, 255, 255, .2) !important;
}
```

- [ ] **Step 2: Build to confirm nothing breaks**

Run: `dotnet build RazorShop.Web/RazorShop.Web.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Visual check**

Run the app (use the `run` skill) and load `/`. Confirm: nav bar is black with white uppercase links, "Shop" dropdown readable, cart link visible; footer is black with white text and uppercase headings; hovering footer links turns them orange-red.

- [ ] **Step 4: Commit**

```bash
git add RazorShop.Web/wwwroot/css/theme-editorial.css
git commit -m "Editorial theme: black nav and footer chrome

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Bold uppercase headings

**Files:**
- Modify: `RazorShop.Web/wwwroot/css/theme-editorial.css` (append)

- [ ] **Step 1: Append the typography rules**

Append to `RazorShop.Web/wwwroot/css/theme-editorial.css`:

```css
/* --- Headings: heavy, uppercase, tight --- */
[data-theme="editorial"] h1,
[data-theme="editorial"] h2,
[data-theme="editorial"] h3,
[data-theme="editorial"] .display-5 {
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: -.01em;
    line-height: 1.05;
}

[data-theme="editorial"] h1,
[data-theme="editorial"] .display-5 {
    letter-spacing: -.02em;
}
```

- [ ] **Step 2: Build to confirm nothing breaks**

Run: `dotnet build RazorShop.Web/RazorShop.Web.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Visual check**

Run the app and load `/` and `/Product/1`. Confirm product name (`h1.display-5` on detail, `h2.h5` on grid) renders bold and uppercase with tight spacing, and nothing wraps awkwardly.

- [ ] **Step 4: Commit**

```bash
git add RazorShop.Web/wwwroot/css/theme-editorial.css
git commit -m "Editorial theme: bold uppercase headings

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Buttons, links, and form inputs

**Files:**
- Modify: `RazorShop.Web/wwwroot/css/theme-editorial.css` (append)

- [ ] **Step 1: Append the button/link/input rules**

Append to `RazorShop.Web/wwwroot/css/theme-editorial.css`:

```css
/* --- Buttons: squared, bold, uppercase, accent primary --- */
[data-theme="editorial"] .btn {
    border-radius: 2px;
    text-transform: uppercase;
    letter-spacing: .04em;
    font-weight: 700;
}

[data-theme="editorial"] .btn-primary {
    --bs-btn-bg: var(--accent);
    --bs-btn-border-color: var(--accent);
    --bs-btn-hover-bg: #e23d20;
    --bs-btn-hover-border-color: #e23d20;
    --bs-btn-active-bg: #c9341a;
    --bs-btn-active-border-color: #c9341a;
    --bs-btn-color: #fff;
    --bs-btn-hover-color: #fff;
    --bs-btn-active-color: #fff;
}

/* --- Inputs: squared, accent focus ring --- */
[data-theme="editorial"] .form-control,
[data-theme="editorial"] .form-select {
    border-radius: 2px;
}

[data-theme="editorial"] .form-control:focus,
[data-theme="editorial"] .form-select:focus {
    border-color: var(--accent);
    box-shadow: 0 0 0 .2rem rgba(255, 77, 46, .25);
}
```

(Body link colour is already handled by `--bs-link-color-rgb` from Task 1; nav and footer links are overridden in Task 2, so no extra `a` rule is needed here.)

- [ ] **Step 2: Build to confirm nothing breaks**

Run: `dotnet build RazorShop.Web/RazorShop.Web.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Visual check**

Run the app. On `/Product/1` confirm the "add to cart"/buy button is orange-red, squared, uppercase, and darkens on hover. On `/checkout` confirm inputs are squared and show an orange focus ring when clicked. Confirm body links (e.g. on `/terms`) are the darker accent `#d6381c`, not the bright fill.

- [ ] **Step 4: Commit**

```bash
git add RazorShop.Web/wwwroot/css/theme-editorial.css
git commit -m "Editorial theme: buttons, inputs, link colours

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Product grid + cards

**Files:**
- Modify: `RazorShop.Web/wwwroot/css/theme-editorial.css` (append)

Note: both the product grid (`Slices/Products.cshtml`) and product detail (`Slices/Product.cshtml`) wrap content in `<section id="section">`, and the grid uses Bootstrap `.card`.

- [ ] **Step 1: Append the card rules**

Append to `RazorShop.Web/wwwroot/css/theme-editorial.css`:

```css
/* --- Product grid cards --- */
[data-theme="editorial"] #section .card {
    border: 1px solid var(--ink);
    border-radius: 2px;
    transition: transform .15s ease;
}

[data-theme="editorial"] #section .card:hover {
    transform: translateY(-3px);
}

[data-theme="editorial"] #section .card a {
    color: var(--ink);
    text-decoration: none;
}

[data-theme="editorial"] #section .card:hover h2 {
    color: var(--accent-text);
}
```

- [ ] **Step 2: Build to confirm nothing breaks**

Run: `dotnet build RazorShop.Web/RazorShop.Web.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Visual check**

Run the app and load `/Products`. Confirm cards have a thin black border, square corners, lift slightly on hover, and the product name turns accent on hover. Card text is ink-coloured (not bright accent) at rest.

- [ ] **Step 4: Commit**

```bash
git add RazorShop.Web/wwwroot/css/theme-editorial.css
git commit -m "Editorial theme: product grid cards

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Full verification pass

**Files:** none (verification only)

- [ ] **Step 1: Run the whole test suite**

Run: `dotnet test RazorShop.Tests/RazorShop.Tests.csproj`
Expected: PASS — all tests green, including `Home_is_wired_to_the_editorial_theme` and the existing smoke/route tests (proves the restyle didn't break page rendering).

- [ ] **Step 2: Full visual sweep**

Run the app (use the `run` skill) and walk through: `/` (home), `/Products` (grid), `/Product/1` (detail), add to cart, `/checkout`, and one info page (`/terms`). Confirm the Editorial Bold look is consistent and nothing is unreadable (white-on-white or accent-on-accent).

- [ ] **Step 3: Confirm clean rollback path (sanity check, do not commit)**

Temporarily remove `data-theme="editorial"` from `_Layout.cshtml`, reload `/`, confirm the original Bootstrap look returns unchanged, then put the attribute back. This verifies the theme is truly additive.

- [ ] **Step 4: Final confirmation**

No commit needed if Tasks 1–5 were each committed. The branch `feature/editorial-bold-restyle` now contains the full restyle.
