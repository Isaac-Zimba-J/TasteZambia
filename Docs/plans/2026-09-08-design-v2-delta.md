# Design v2 — what changed, and what it costs the plan

**Imported:** 2026-09-08 from Claude Design project `e2f2819c-ffd4-4664-a4bf-eccbd6e95261`.
**Spec now:** `Docs/Mobile app design project/Taste Zambia.dc.html` (212,439 bytes).
**Superseded:** `Taste Zambia.v1-superseded.dc.html` (121,854 bytes) — kept only for diffing.

The canvas went from **11 screens to 33**, organised into five labelled rows.

---

## 1. Seed data did not move — at all

Every content block diffs byte-identical between v1 and v2:

`DISHES` · `INGREDIENTS` · `PROVINCES` · `ARTICLES` · `STEPS` · `VARIATIONS`
`RECIPE_INGREDIENTS` · `CATS` · `FILTERS` · `STORIES` · `SHARE_STEPS`

**Consequence:** Task 2 (models), Task 3 (repositories + `SeedData`) and Task 4 (services)
need **no changes**. That is the bulk of the transcribed content and it survives intact.

## 2. Two breaking changes to already-written tasks

### 2a. A systematic contrast pass rewrote six tokens (Task 1)

This is broader than it first looked. v2 darkened every mid-tone that carried text, and
collapsed two of them together. Counts are occurrences in each file.

| Role | v1 | v2 | Plan token |
|---|---|---|---|
| Gold actions / gold text | `#C07F1E` (25) | **`#8A5A12`** (35) | `TzGold` |
| Gold pressed | `#A96D13` | **`#6F4A10`** (13) | `TzGoldPressed` |
| Text on gold tint | `#8A6A1C` (3) | **`#7A5A10`** (6) | `TzGoldTintText` |
| Meta / mono labels | `#8B7C68` (25) | **`#6B5C4A`** (33) | `TzMuted2` |
| Faint labels | `#9C8D79` (14) | *gone* | `TzFaint` |
| Chevron glyphs | `#C4B8A4` (3) | *gone* | `TzChevron` |
| Muted body | `#7A6B59` (26) | `#7A6B59` (**84**) | `TzMuted` |

`#C07F1E` survives, but **only as decoration** — 7px progress dots, 3px track fills and the
palette swatch. It is no longer legal for text or a CTA fill. The plan now carries it as a
separate `TzGoldDecor` token.

`TzFaint` and `TzChevron` no longer exist as distinct values: both collapsed into
`TzMuted`, which is why its usage tripled. The plan keeps both keys defined as **aliases**
of `#7A6B59` so the XAML already written in Tasks 6–17 needs no edit and is still correct.

Two new roles appear: `TzTimelineIdle` `#D8CDB9` (a not-yet-reached timeline dot) and
`TzClayText` `#8F3B23` ("changes requested" badge text).

**Already applied** to `Colors.xaml`, the Global Constraints table, the gold-usage rule,
`WizardProgress`'s track fill, and `ContributionRowViewModel`'s status hexes in Task 17.

### 2b. The nav mapping grew (Task 5)

`NAV.profile.also` went from `['family','share']` to seventeen entries: every share-flow,
preserve-flow and collections screen keeps **Profile** lit. The `BottomNavBar`
section-mapping table in Task 5 must list them.

Rows 1 (onboarding) shows **no nav bar at all** — it is pre-Shell.

## 3. Twenty-two new screens

The 11 already planned map exactly onto row 2 plus `share` and `family`. New work:

| Row | Screens | Notes |
|---|---|---|
| **Onboarding — first launch** (7) | `splash`, `intro`, `onbLang`, `onbWho`, `onbTaste`, `onbNotify`, `onbReady` | New state: `lang`, `who`, `tastes{}`. No bottom nav. Runs before Shell. |
| **Share a Recipe** (+5) | `shareStart`, `shareDraft`, `shareReview`, `shareChanges`, `sharePublished` | The existing `share` form is the middle step of a six-screen lifecycle. |
| **Preserve a Family Recipe** (+5) | `famStart`, `famDraft`, `famSaved`, `famShared`, `famPublic` | Adds audio-pending draft state and three privacy outcomes. |
| **Profile collections** (5) | `favs`, `wantTry`, `cooked`, `famList`, `settings` | The four collection rows and Settings row on Profile now open real screens. |

### New onboarding content (from the canvas)

- **Languages** (`onbLangs`) — selectable list, gold-ringed when active on a dark ground
  (`border #e8bd77`, `background rgba(232,189,119,.16)`).
- **Who you are** (`onbWhos`) — radio list, `#eef2ec` fill + `#17402f` ring when selected.
  Default `'I grew up here'`.
- **Tastes** (`onbTastes`) — multi-select chips, inverted to `#17402f` when on, with a
  `"N selected"` counter. Defaults: `traditional`, `veg` on.

## 4. What this costs

- **Tasks 2, 3, 4** — unchanged. All eleven seed-data blocks diff byte-identical.
- **Task 1** — six token corrections (2a). Applied.
- **Task 5** — nav mapping expanded (2b). Applied.
- **Tasks 6–17** — unchanged; `TzFaint`/`TzChevron` kept as aliases so the XAML still holds.
- **Task 17** — status hexes corrected. Applied.
- **Tasks 19–22** — written, covering all 22 new screens.

The plan is now **22 tasks / 167 steps** covering all 33 screens. Nothing already written
was wasted.

## 5. Open items carried into Part 2

1. **`OnboardingViewModel` is a Singleton** for the onboarding run, but the app root swaps
   to `AppShell` on completion. If onboarding is ever re-entered from Settings, reset it or
   make it Scoped — otherwise stale selections persist.
2. **This directory is not a git repository.** Every "Commit" step in the plan will fail
   until `git init` is run.
3. **Audio is still visual only.** Three screens now show recordings (`famDraft`,
   `famShared`, `story`) and two offer to record one. Wiring capture and playback is a
   feature task, not part of the UI phase.
4. `ReviewStage` (Task 15) and `ReviewStep` (Task 20) coexist deliberately: the former is
   the static pipeline explainer shown *before* submitting, the latter the live timeline
   shown *after*.
