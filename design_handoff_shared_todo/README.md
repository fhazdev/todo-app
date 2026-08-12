# Handoff: Sprout — shared todo lists with list types and per-type categories

## Overview
Sprout is a mobile (Android-first) todo app. A user signs in with Google or an email
account, keeps several **lists**, and shares a list with other people. Every list has a
**list type** (Grocery list, Movie & show list, Default list, or a type the user creates).
The type owns a user-defined, ordered set of **categories**; each item on a list takes one
of its type's categories. Items toggle between open and completed and back. A sort menu
switches between the custom category sort (grouped by the type's category order) and
normal sorts (my order, due date, alphabetical).

The chosen direction is **1a / 2a** ("Familiar"): bottom tab bar, one flat scrolling list,
category shown as a coloured chip per row, category headers inserted in place when the
custom sort is active, and completed items collapsed at the bottom of the list.

## About the design files
`Todo App Directions.dc.html` in this bundle is a **design reference created in HTML** — a
prototype showing intended look and behaviour, not production code to copy. The task is to
**recreate these designs in the target codebase's existing environment** (React Native,
Kotlin/Compose, SwiftUI, Flutter, web React…) using its established patterns, component
library, navigation and state tooling. If no environment exists yet, pick the framework
that fits the product and implement the designs there.

The file is a single-file streaming prototype: markup in one template, behaviour in one
`class Component` (state, sorting, toggling, category/type mutation). Read the class for
the exact logic; read the template for exact styles. Everything is inline-styled with
literal hex values taken from the design system's stylesheet, which is also bundled
(`styles.css`).

## Fidelity
**High fidelity.** Colours, type, radii, spacing and copy are final-intent. Recreate the UI
faithfully using the codebase's own primitives; the exact hex values, font sizes and radii
are listed below and in the prototype's inline styles. Layout numbers assume a 412 × 892
Android viewport (the device frame in the prototype is a mock bezel — do not port it).

Note on the prototype file: it contains three explored directions (turn 1: `1a` Familiar,
`1b` Bands, `1c` Beds) plus the accepted, most current design (turn 2, option `2a`).
**Implement `2a`.** `1b` and `1c` are rejected alternatives kept for reference only.

---

## Screens / views

Navigation model: a bottom tab bar (Lists / Today / Types) on the home screen; list detail,
new list, list types, type categories and share are pushed screens with a back affordance
at the top left. Sort and Add item are bottom sheets over the list detail.

### 1. Sign in / create account
- **Purpose**: authenticate with Google or with an email + password account.
- **Layout**: single column, vertically centred, page padding 28px horizontal / 40px bottom,
  16px gaps. Ground `#f5ead8`.
- **Components**
  - Brand mark: 72 × 72 circle, `#c67139`, containing a 28 × 28 shape with radius
    `999px 999px 999px 4px` in `#f5ead8` (a leaf/sprout glyph — replace with the real logo).
  - Title "Sprout": Caprasimo 38px, line-height 1.05, `#201e1d`.
  - Subtitle "Lists you keep together.": Figtree 15px, `#201e1d` at 60% opacity.
  - Google button: full width, min-height 52px, radius 999px, surface `#ebddc5`, 1px divider
    border `rgba(32,30,29,.16)`, label Caprasimo 15px, with a 24px circular badge (`#f5ead8`,
    Figtree 700 13px, `#645c50`) standing in for the Google mark — use the real Google
    branding asset in production.
  - Divider row: 1px `rgba(32,30,29,.14)` rules either side of the 11px label "or use email".
  - Two inputs (email, password): full width, min-height 50px, radius 999px, background
    `#ebddc5`, 1px `rgba(32,30,29,.16)` border, 14px text, 14px horizontal padding,
    placeholders "you@email.com" and "Password".
  - Primary button "Create account": full width, min-height 52px, radius 999px, fill
    `#c67139`, label `#f5ead8` Caprasimo 15px. Hover `#b2622d`, pressed `#8c491a`.
  - Ghost link "I already have an account": Caprasimo 13px, `#c67139`, min-height 44px.
- **Behaviour**: any of the three actions authenticates and navigates to Lists home
  (the prototype fakes it; wire to real Google OAuth + email/password).

### 2. Lists home
- **Purpose**: see every list the user owns or is a member of; open one; create one.
- **Layout**: header block (22px top / 20px sides / 8px bottom), scrolling list of cards
  (20px sides, 12px gaps), bottom tab bar pinned.
- **Components**
  - Title "My lists": Caprasimo 29px. Subtitle: account email, Figtree 12.5px at 55%.
  - **List card** (one per list): row, min-height 82px, padding 15px, radius 28px, surface
    `#ebddc5`, 14px gap.
    - Type icon: 44 × 44, radius `999px 999px 999px 8px`, filled with the type's first
      category colour.
    - Name: Caprasimo 17px. Below it a row with the **type chip** (Figtree 10.5px, padding
      2px 9px, radius 999px, background = type tint, text = type deep tone) and a meta
      string `"<n> left · shared with <n>"` or `"<n> left · just you"` (12px, 55%).
    - Member avatars, right: 26 × 26 circles, radius 999px, Figtree 700 10px, `#f5ead8`
      text, 2px `#ebddc5` border, overlapped by `margin-left:-8px` after the first.
  - Primary button "＋ New list": full width, min-height 52px.
  - **Bottom tab bar**: surface `#ebddc5`, padding 8px 16px 12px, three equal items,
    min-height 56px each; icon 22 × 22 with 2.75px stroke, label Figtree 10.5px. Active
    tab `#c67139`, inactive `rgba(32,30,29,.45)`. Tabs: Lists (active), Today, Types.
- **Behaviour**: tapping a card opens that list; "＋ New list" opens the New list screen;
  the Types tab opens List types.

### 3. List detail
- **Purpose**: work the list — read, tick, add, re-sort, reach sharing and categories.
- **Layout**: fixed header block on `#ebddc5` (padding 14px 18px 12px, 8px gaps), scrolling
  item area (6px top, 18px sides, 96px bottom so the FAB never covers the last row),
  floating action button bottom-right.
- **Header components**
  - Back link "‹ Lists": Figtree 13px, `#8c491a`, min-height 44px.
  - Member avatar stack, right: 30 × 30 circles, 700 11px, 2px `#ebddc5` border,
    `margin-left:-9px` overlap. Tapping opens Shared with.
  - List name: Caprasimo 26px.
  - Type line: `"<Type name> · edit categories"`, Figtree 12px, `#8c491a` — navigates to
    that type's category screen.
  - Sort button: secondary pill, min-height 40px, 12.5px, background `#f5ead8`, label
    `Sort: <Category | My order | Due date | Alphabetical> ▾`; beside it `"<n> left"` (12px, 55%).
- **Item rows**
  - Row: flex, 13px gap, padding 12px 4px, min-height 58px, 1px bottom rule
    `rgba(32,30,29,.08)`.
  - Checkbox: 26 × 26 circle, 2.75px border in the item's category colour; when completed
    the circle fills with that colour and shows an 11 × 6 white-ish tick (two 2.75px borders,
    rotated −45°, `#f5ead8`). Whole circle is the hit target — enlarge to 44px minimum in
    production.
  - Title: Figtree 15.5px, line-height 1.3. Completed: `line-through` + 45% opacity.
  - Meta row (5px above): category chip (11px, padding 3px 9px, radius 999px, background
    category tint, text category deep tone, with an 8px colour dot) and due-date text
    (11px, 50%).
- **Category headers** (only when sort = By category): flex row, padding 14px 4px 6px,
  Caprasimo 14px in the category's deep tone, with a 10px colour dot on the left, a 1px
  hairline in `<category colour>55` filling the middle, and the group count (Figtree 11.5px,
  60%) at the right.
- **Completed section**: a text button `"Completed (n) ▾"` (Figtree 13px, `#645c50`,
  min-height 48px, 20px top margin) that expands/collapses the completed rows. Completed
  rows have no bottom rule.
- **FAB**: "＋ Add item", primary pill, height 58px, horizontal padding 22px, 15px label,
  shadow `0 12px 32px rgba(46,43,37,.22)`, offset 20px right / 22px bottom.
- **Empty-category rule**: a category with no items in the current filter renders no header.
- **Uncategorised rule**: if every item on the list is in a single category and that category
  is the type's catch-all ("Uncategorised"), the list shows **no** category chrome at all —
  no headers, no chips, no dots — and checkboxes fall back to the accent `#c67139`. As soon
  as one item takes a real category, grouping returns.

### 4. Sort sheet
- Bottom sheet over a `rgba(46,43,37,.45)` scrim; surface `#ebddc5`, radius `32px 32px 0 0`,
  padding 18px 14px 26px. Tapping the scrim dismisses.
- Title "Sort by": Caprasimo 19px.
- Four options, each a row: min-height 56px, padding 14px 18px, radius 999px, 12px gap;
  selected row background `#ffe1d0`. Radio mark: 18px circle, 2.75px `#c67139` border,
  filled `#c67139` when selected. Label Figtree 14.5px + note 11.5px at 55%.
  - By category (custom) — note: "<Type name> categories, your order"
  - My order — "As you added them"
  - Due date — "Soonest first"
  - Alphabetical — "A to Z"
- Footer ghost button "Edit this type's categories" → type category screen.

### 5. Add item sheet
- Same sheet shell, padding 18px, 12px gaps.
- Title "New item" (Caprasimo 19px); text input (min-height 50px, background `#f5ead8`,
  placeholder "What needs doing?"); an uppercase 11px label `"<Type name> categories"`
  (letter-spacing .08em, 50%); a wrapping row of category chips (min-height 40px, padding
  8px 14px, radius 999px, 13px) — unselected: background category tint, text category deep
  tone; selected: background category colour, text `#f5ead8`; then Cancel (secondary) and
  Add (primary) buttons, each `flex:1`, min-height 50px.
- **Behaviour**: Add appends an open item with the chosen category to the current list and
  closes the sheet; empty input closes without adding. Category defaults to the type's
  first category. (Due date is not in this prototype — see Open questions.)

### 6. New list
- **Purpose**: name a list and choose its type.
- Header: back "‹ Lists", title "New list" (Caprasimo 26px), helper "The type decides which
  categories items can take." (12.5px, 60%).
- Name input: min-height 52px, background `#ebddc5`, placeholder "List name".
- Uppercase label "LIST TYPE" (11px, .08em, 50%).
- Type option cards: row, min-height 76px, padding 14px, radius 26px, 13px gap; unselected
  background `#ebddc5` with a transparent 2.75px border; selected background = type tint with
  a 2.75px border in the type colour. Contains the 38px type icon, the type name (Caprasimo
  16px) and its categories joined with " · " (11.5px, 60%).
- Ghost button "＋ New list type" → List types.
- Footer bar (`#ebddc5`, padding 12px 18px 18px) with primary "Create list" (min-height 54px).
- **Behaviour**: creates an empty list of the chosen type, navigates straight into it. Empty
  name falls back to "Untitled list".

### 7. List types
- **Purpose**: manage the types and create new ones.
- Header: back "‹ Lists", title "List types", helper "Each type carries its own categories."
- Type row (tappable): min-height 78px, padding 14px, radius 26px, `#ebddc5`; 40px type icon;
  name Caprasimo 16px; meta `"<n> categories · <n> lists"` (11.5px, 55%, correctly singular
  at 1); right side a stack of 16px category swatches, 2px `#ebddc5` border, `-5px` overlap.
- Create-type patch: radius 26px, background `#ffe1d0`, padding 16px, 10px gaps: heading
  "Add a type of your own" (Caprasimo 16px, `#8c491a`), input (placeholder "e.g. Reading
  list", background `#f5ead8`), primary "Create type" (min-height 48px).
- **Behaviour**: creating a type seeds it with a single category named "Uncategorised" and
  opens its category screen. Tapping a type row opens its category screen.

### 8. Type categories
- **Purpose**: define and order the categories of one type. **This order is the custom sort**
  for every list of that type.
- Header on `#ebddc5`: back "‹ List types", title = type name (Caprasimo 26px), helper
  "This order is what “By category” sorts on, for every list of this type."
- Category row: min-height 66px, padding 12px 14px, radius 26px, `#ebddc5`, 12px gap:
  position number (Caprasimo 13px, 45%, 12px wide), 32px colour swatch circle, name
  (Caprasimo 15.5px), then two 38 × 38 icon buttons ▲ / ▼ (secondary, background `#f5ead8`)
  that move the category up/down.
- Add row: input "New category" + primary "Add" (both min-height 50px).
- **Behaviour**: new categories are assigned the next colour from the palette cycle
  (index = current count mod 6). ▲ on the first row and ▼ on the last are no-ops — disable
  them in production. Rename and delete are not in the prototype (see Open questions).

### 9. Shared with
- Header on `#ebddc5`: back "‹ <List name>", title "Shared with".
- Member row: min-height 68px, padding 12px 14px, radius 26px, `#ebddc5`, 13px gap: 40px
  avatar circle (700 13px, `#f5ead8` text, member colour fill), name (14.5px, ellipsised)
  and role (11.5px, 55%) — "Owner · you", "Can edit", "Invited"; invited members show the
  email address instead of a name.
- Primary "Invite by email" (min-height 52px).
- **Behaviour**: static in the prototype. Membership model to implement: owner + editors;
  invitations by email that are pending until accepted.

---

## Interactions & behaviour
- **Toggle complete**: tapping an item's circle flips `done`. Open items live in the main
  body; completed items move into the collapsible "Completed (n)" section at the bottom
  and are struck through at 45% opacity. Re-tapping a completed item returns it to the open
  body immediately. Both directions are shared state — in a shared list this must sync to
  all members.
- **Sorting**: the sort selection is per view in the prototype (one setting for the current
  list); persist it per list in production.
  - By category: items grouped in the type's category order; headers rendered per non-empty
    group; within a group the original order is kept.
  - My order: insertion order.
  - Due date: items with a due date first (alphabetical inside the prototype's simple
    comparator — implement a real date comparator), undated after.
  - Alphabetical: A→Z by title, case-insensitive locale compare.
- **Adding**: new items are always open, take the selected category and no due date, and are
  appended (so they land at the end of "My order" and inside their category group).
- **Category reorder**: moving a category up or down in the type immediately changes the
  grouped order in every list of that type.
- **Navigation**: back links pop to the parent screen; the sort/add sheets are modal over the
  list. No page transitions are specified — use the platform default push/modal animation.
- **Hover / pressed / focus** (from the design system, apply everywhere):
  - Primary: fill `#c67139`, hover `#b2622d`, pressed `#8c491a`.
  - Secondary: hover `rgba(32,30,29,.07)`, pressed `rgba(32,30,29,.14)`.
  - Ghost: text `#c67139`, hover background `rgba(198,113,57,.10)`, pressed `.18`.
  - Focus-visible: `2px solid #c67139`, offset 2px. Never the platform default blue ring.
  - Disabled: 45% opacity.
- **Hit targets**: every interactive row/button is at least 44px tall in the design; keep that
  floor, and enlarge the 26px checkbox's touch area to 44px.
- **Loading / error / empty states**: not designed. Minimum needed: list-loading skeleton,
  offline/sync-failure banner on a shared list, empty-list state ("Add your first item"),
  invite failure message, and validation on the auth form (email format, password length)
  and on new type/category names (non-empty, no duplicate name within the type).

## State management
Prototype state, per app instance (`mk2()` in the logic class):
- `screen` — one of `home | list | new | types | typecats | share` (plus the sign-in screen).
- `sheet` — `null | 'sort' | 'add'`.
- `sort` — `custom | my | due | alpha`.
- `showDone` — completed section expanded.
- `listId` — the open list; `editTypeId` — the type being edited.
- `types: [{ id, name, blurb, cats: [{ id, name, pi }] }]` — `cats` order **is** the custom
  sort order; `pi` indexes the colour palette.
- `lists: [{ id, name, typeId, shared, items: [{ id, text, cat, due, done }] }]` — `cat` is a
  category id belonging to the list's type.
- Draft state: `draft`, `draftCat` (add item), `newList {name, typeId}`, `newTypeName`,
  `newCatName`.

Transitions: `signIn → home`; card tap → `list` (sets `listId`); avatar tap → `share`;
type line / sort footer → `typecats` (sets `editTypeId` to the list's type); tab Types →
`types`; create type → `typecats` for the new type; create list → `list` for the new list.

Real implementation needs: authenticated user, per-list membership, server-side list/item/type
persistence, realtime sync of item completion and additions, and optimistic local toggles.
Categories and types are user data scoped to the account (decide whether a shared list's type
is shared with its members — see Open questions).

## Design tokens
From the bundled `styles.css` (Organic design system). Prefer these variables over literals.

Core: `--color-bg #f5ead8`, `--color-surface #ebddc5`, `--color-text #201e1d`,
`--color-accent #c67139`, `--color-accent-2 #7a8a5e`,
`--color-divider rgba(32,30,29,.16)`.

Ramps used: neutral `#f9f4ed #eee7db #dcd3c4 #c0b6a5 #a19786 #82796a #645c50 #474238 #2e2b25`;
accent `#fff2eb #ffe1d0 #ffc6a5 #f6a06b #d67f48 #b2622d #8c491a #643312 #402310`;
accent-2 `#f0fae1 #e1eecc #ccdbb2 #aebf92 #8fa073 #728157 #56633f #3d472b #272e1b`.

Category palette (colour / tint / deep — cycled by index for new categories):
1. `#c67139` / `#ffe1d0` / `#8c491a`
2. `#7a8a5e` / `#e1eecc` / `#56633f`
3. `#b2622d` / `#fff2eb` / `#643312`
4. `#82796a` / `#eee7db` / `#474238`
5. `#f6a06b` / `#fff2eb` / `#8c491a`
6. `#56633f` / `#f0fae1` / `#272e1b`

Member avatar colours: `#c67139` (MK), `#7a8a5e` (NB), `#82796a` (SO).

Type: headings Caprasimo 400 (line-height 1.12, letter-spacing −0.015em) at 38 / 29 / 26 /
19 / 17 / 16 / 15.5 / 14px as noted per screen; body Figtree 400/600/700 at 15.5px items,
14.5px rows, 13px links, 12.5px meta, 11.5px sub-meta, 11px chips and due dates, 10.5px tab
labels. Uppercase micro-labels: 11px, letter-spacing .08em.

Spacing scale: 4.4 / 8.8 / 13.2 / 17.6 / 26.4 / 35.2px (`--space-1…8`). Screen padding
18–20px horizontal.

Radius: `--radius-sm 8px`, `--radius-md 16px`, `--radius-lg 28px`; cards 26–32px; buttons,
inputs, chips and tags `999px`; sheets `32px 32px 0 0`; type/list icons
`999px 999px 999px 8px`.

Shadows: `--shadow-sm 0 1px 2px rgba(46,43,37,.14)`, `--shadow-md 0 3px 10px rgba(46,43,37,.16)`,
`--shadow-lg 0 12px 32px rgba(46,43,37,.22)` (the FAB uses the lg value).

Icon style: Lucide, stroke-width 2.75.

## Content used in the prototype
- Types and categories: **Grocery list** — Fresh produce, Bread & bakery, Dairy, Meat & fish,
  Pantry. **Movie & show list** — Films, Series, Documentary, With the kids.
  **Default list** — Errands, House, Food, Admin.
- Lists: *Groceries* (grocery, shared with 2), *Weekend at the cabin* (default, shared with 3),
  *Friday film night* (movie & show, shared with 2).
- Members: Maya Kern (owner, you), Nina Boye (can edit), sam.oyelaran@gmail.com (invited).

## Assets
No image assets. Everything is CSS shapes and text:
- The brand mark, type icons and list icons are asymmetric rounded squares/circles standing in
  for real icons — replace with the product's icon set (Lucide, stroke-width 2.75) and a real
  logo.
- The Google button's "G" badge is a placeholder; use Google's official branding asset and
  follow their sign-in button guidelines.
- Checkbox ticks, category dots and progress rings are pure CSS.
- Fonts: Caprasimo 400 and Figtree 400/600/700, loaded from Google Fonts in `styles.css`.
- The Android bezel in the prototype is a mock device frame for presentation only.

## Open questions for the developer / designer
1. A list cannot currently override its type's categories — confirm that's the intended model.
2. Renaming and deleting a category (and what happens to items in a deleted category) is not
   designed.
3. Due dates are displayed but cannot be set in the Add item sheet.
4. The Today tab, notifications, and per-item assignees are out of scope so far.
5. Whether a shared list's type/categories are visible and editable by its members.

## Files
- `screens/` — reference screenshots of option `2a` at 2× (824 × 1784, inside the mock Android
  bezel — ignore the bezel, status bar and gesture bar):
  - `00-sign-in.png` — sign in / create account (captured from `1a`; unchanged in `2a`)
  - `01-lists-home.png` — Lists home with type chips, member avatars and the bottom tab bar
  - `02-list-detail-category-sort.png` — list detail, By-category sort with headers
  - `03-sort-sheet.png` — sort bottom sheet
  - `04-add-item-sheet.png` — add item sheet with the type's category chips
  - `05-completed-expanded.png` — completed section expanded
  - `06-type-categories.png` — categories of a type, with the reorder controls
  - `07-list-types.png` — list types incl. the create-a-type patch
  - `08-new-list.png` — new list: name + type picker
  - `09-shared-with.png` — members and roles
- `Todo App Directions.dc.html` — the design prototype. Implement turn 2, option `2a`
  (the top-most section). Turn 1 options `1a`, `1b`, `1c` are earlier explorations; `1a` is
  the accepted shell that `2a` builds on.
- `styles.css` — the Organic design system stylesheet: tokens, ramps, and the `.btn` /
  `.input` / `.card` / `.tag` / `.seg` / `.dialog` component classes the prototype composes with.
- `android-frame.jsx` — the mock Android bezel used for presentation. Not part of the app.
- To open the prototype, serve the folder and load the HTML file in a browser.
