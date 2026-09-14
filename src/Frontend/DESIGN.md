# Telara — Design handoff

Constraint-visibility UI for discrete manufacturing. Three role-scoped views over the same
production model; each role sees the plant at a different altitude.

Source of truth for this doc: the `.dc.html` design files in this project. Where this doc and a
design file disagree, the design file wins.

---

## 1. Roles and altitude

| Role | Altitude | Primary question | Primary view |
| --- | --- | --- | --- |
| Station Supervisor | One line, one shift, station-by-station | Which station is holding up my line right now? | Isometric station flow |
| Shift Manager | Multiple lines, one shift, station as a single unit | Which lines are healthy, and is anything upstream of them common? | Line units + constraint-type set diagram |
| Plant Director | Whole facility, across shifts and weeks | Is the plant trending toward or away from commitment? | Charts, trends, part table |

Role determines the default route after auth. Roles are hierarchical for read access
(Director ⊇ Manager ⊇ Station Supervisor) but **not** for default landing — each lands on its own view.

### Routes

    /                        → redirect to role default
    /station                 → Station Supervisor dashboard   (line + part scoped)
    /station/:stationId      → station detail (planned)
    /shift                   → Shift Manager dashboard        (shift scoped, all lines)
    /overview                → Plant Director dashboard       (facility scoped)
    /overview/parts/:partId  → part drill-down (planned)
    /alerts                  → shared, filtered by role scope
    /personnel               → shared, filtered by role scope
    /financials              → Director only, planned

Auth validation only needs the three role defaults to resolve and render their blank shell.

---

## 2. Color

Two modes. Dark "control room" is the default; light "blueprint" is for tablets and laptops on the
floor. Text tokens below meet at least 4.5:1 against their stated background — `text/dim` is the
one exception and is **not** a text token (see its row).

### Dark mode

| Token | Hex | Use |
| --- | --- | --- |
| `bg/page` | `#141416` | page behind the app frame |
| `bg/app` | `#242424` | app frame |
| `bg/chrome` | `#1b1b1d` | header, nav, timeline bar |
| `bg/panel` | `#2b2b2e` | cards, panels |
| `border/strong` | `#08060d` | header baseline rule |
| `border/default` | `#3f3f45` | panel + nav borders |
| `border/subtle` | `#333337` | in-panel dividers, track fills |
| `text/primary` | `#f2f3f5` | headings, values |
| `text/secondary` | `#a7acb5` | body, labels, mono eyebrows, axis ticks, data bars, set-diagram strokes (6.19:1 on `bg/panel`) |
| `text/dim` | `#6b6d74` | **rules and non-informational fills only** — 2.73:1 on `bg/panel`, so never for text a user has to read. Labels, eyebrows, axis ticks and hold reasons all use `text/secondary`. |
| `accent/blue` | `#8fb7ef` | primary action, boundary nodes, selection |
| `status/ok` | `#1ed488` | operational, on target |
| `status/watch` | `#d9a712` | idle, drifting |
| `status/warn` | `#f5993d` | warning |
| `status/critical` | `#f39991` | constraint, threshold crossed |
| `status/critical-edge` | `#971c11` | critical card border |

### Light mode

| Token | Hex | Use |
| --- | --- | --- |
| `bg/page` | `#eeeeee` | page, and blueprint grid ground |
| `bg/panel` | `#ffffff` | cards, nav, header |
| `grid/line` | `#c9d3e6` | 24px blueprint grid |
| `border/default` | `#d7dbe3` | panel + nav borders |
| `border/subtle` | `#eef0f3` | in-panel dividers, track fills |
| `text/primary` | `#17181b` | headings, values |
| `text/secondary` | `#5b5f68` | body, labels, axis ticks |
| `accent/blue` | `#154991` | primary action, boundary nodes |
| `status/ok` | `#0d5939` | operational, on target |
| `status/watch` | `#634c08` | watch |
| `status/warn` | `#794006` | warning, idle |
| `status/critical` | `#971c11` | constraint, threshold crossed |

Status colors are **never** the only signal — every status is also carried by a text label
(`operational`, `backlog +41`, `CRITICAL`).

### Isometric prism faces

Each station node is three faces of one status hue, all at `0.5` fill opacity with a solid `3px`
stroke of the same value. Top face is the lightest, left the darkest.

| Status | Top | Left | Right |
| --- | --- | --- | --- |
| ok | `#22e08f` | `#0e6b46` | `#189a63` |
| watch | `#f0b429` | `#8a5f08` | `#c98a12` |
| critical | `#ef5b4e` | `#8a1f16` | `#c93b2e` |
| boundary (in/out) | `#7fb0ef` | `#1c4d94` | `#2f6fd6` |

Prism **height** encodes deviation magnitude; **hue** encodes severity type. Fill opacity is
reserved for condition severity (not yet wired — currently constant at 0.5).

---

## 3. Type

- Display / numerics: **Space Grotesk** 500/600/700, `letter-spacing: -0.02em` at 26px+
- UI / body: **Inter** 400/500/600/700
- Labels, ticks, timestamps, badges: **Space Mono** 400/700, uppercase, `letter-spacing: 0.06em`

| Role | Family | Size | Weight |
| --- | --- | --- | --- |
| KPI value | Space Grotesk | 32 | 600 |
| Hero headline | Space Grotesk | 26 | 600 |
| Panel title | Inter | 13 | 600 |
| Body / table cell | Inter | 14 | 400 |
| Meta / secondary | Inter | 12–13 | 400 |
| Mono eyebrow | Space Mono | 10–11 | 400 |

---

## 4. Layout and spacing

- Spacing scale: 4 / 8 / 12 / 16 / 24 / 32
- Radii: 2 (badges) / 4 (controls, nav items) / 6 (panels, app frame) / 999 (avatars, tracks)
- Max content width 1440, page gutter 24
- Panel padding 24; dense panels 18 / 24
- Grid gap between panels: 16

### Nav

Left sidebar, 220px expanded / 64px collapsed, hamburger toggle in the sidebar header.
The sidebar header block is `min-height: 91px` so its bottom rule **aligns with the shift/period
timeline rule** in the main column (header 62px + timeline 31px). That rule is `border/default`,
not the heavy `border/strong` — the heavy rule is only the main-column header baseline.

Collapsed rail uses 1–2 character initials with `title` tooltips; icon set is Font Awesome (Free)
once wired.

### Chrome stack (all three views, top to bottom)

1. Main header — facility, role, scope selects, clock. Baseline `2px solid border/strong`.
2. Timeline / period bar — mono range labels, 4px track, position marker. Baseline `1px`.
3. Content, padded 24. Light mode only: 24px blueprint grid behind content.

---

## 5. Components

| Component | Status | Notes |
| --- | --- | --- |
| AppFrame | built | rounded 6 frame, nav + main split |
| SideNav | built | expanded/collapsed, 91px header block |
| MainHeader | built | scope selects + clock |
| TimelineBar | built | shift range (Station/Manager) or week range (Director) |
| ConstraintHero | built | critical-bordered card, dark mode adds outer glow |
| StationNode | built | 3-face isometric prism + shadow + label pair |
| StationFlow | built | linear only; branching is future work |
| AlertList | built | time · severity badge · message |
| StaffingBars | built | assigned/required with fill bar |
| KpiTile | built | eyebrow, value, delta, note, sparkline |
| BarChart | built | Director throughput vs. target, SVG, value-in-bar labels |
| ShareBars | built | horizontal share-of-hours bars |
| DataTable | built | mono numerics, right-aligned, status badge column |
| PlannedStrip | built | dashed border + PLANNED tag for unbuilt scope |
| LineUnitCard | built | a line as a single unit: status, part, throughput, hold reason |
| IngestionRoot | built | blue boundary prism + cross-line rollup banner + order action |
| SetDiagram | built | three overlapping diamonds, counts per region, critical region ringed |
| HandoffBar | built | shift-1/shift-2 overlap band with carry count |
| HandoffList | built | scope · category badge · what shift 2 inherits |

---

## 6. Open items

1. **Set-diagram scope** — MVP covers two of four readings: **constraint type overlap**
   (capacity / material / labor, with the intersections carrying the interesting cases) and
   **shift handoff overlap** (what shift 2 inherits unresolved). Deferred: lines × shifts × parts,
   and station contention across concurrent part runs. The end state covers all four.
2. **Cross-line constraint rule** (agreed): per-line constraints stand alone. A constraint common
   to multiple lines is resolved upstream — it rolls up to the raw-material ingestion node as root
   and raises an order rate/volume signal. No line-to-line comparison, since outputs are not
   commensurate.
3. **Director drill-down** — part row → line view, to judge whether a part is optimized, needs
   further optimization, or should have its dock expectation lowered.
4. **Revenue / P&L** — planned Director extension; drives material purchase and personnel
   allocation arguments.
5. **Herbie constraint reasoning** — waits on the Sonnet server; UI currently renders a supplied
   constraint verdict rather than deriving one.
6. **Prism fill opacity** — wire to condition severity.
7. **Director throughput chart** — bars currently sit short of the card's full height; widen the
   plot to fill the card.
8. **Light mode** — built for Station Supervisor only. Manager and Director need the light
   "blueprint" pass using the §2 light tokens; no layout changes.

---

## 7. Assets

    Assets/telara-mark.svg            converging-nodes mark
    Assets/telara-lockup-dark.svg     mark + wordmark, dark backgrounds
    Assets/telara-lockup-light.svg    mark + wordmark, light backgrounds
    Assets/telara-wordmark-dark.svg   wordmark only, dark
    Assets/telara-wordmark-light.svg  wordmark only, light

Nav uses the lockup at `height: 20px`.

---

## 8. Design files

    Telara Brand Foundations.dc.html              logo, mark construction
    Telara Design System.dc.html                  tokens, type, components
    Telara Station Supervisor Dashboard v2.dc.html  dark + light, isometric flow
    Telara Shift Manager Dashboard.dc.html        dark; light pending
    Telara Plant Director Dashboard.dc.html       dark; light pending

Each is a self-contained HTML file — open directly in a browser to read exact values off the
rendered markup. All styling is inline; there is no stylesheet to port.
