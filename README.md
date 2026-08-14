# Sprout

Shared todo lists, where a list has a **type** and the type owns an ordered set of
**categories**. That order is the custom sort for every list of that type, so
re-ordering categories once re-groups every list at once.

Built to the handoff in [`design_handoff_shared_todo/`](design_handoff_shared_todo/),
option `2a`. The design is Android-first at 412 × 892, so the web app is mobile-first
and sits in a centred column of that width on a desktop browser.

---

## Stack

| | |
|---|---|
| API | .NET 10, ASP.NET Core controllers, MediatR, FluentValidation |
| Data | EF Core 10 on Npgsql, **Flyway owns the schema** |
| Auth | ASP.NET Identity, self-issued JWT with rotating refresh tokens, Google sign-in |
| Web | React 19, TypeScript, Vite, Tailwind v4, TanStack Query, React Router 7 |
| Database | Neon Postgres in production, a Postgres container locally |
| Tests | xUnit (95) and Jest + Testing Library (59) |
| Hosting | Docker containers on a VPS, nginx serving the SPA and proxying the API |

---

## Getting started

```bash
cp .env.example .env          # optional locally; the compose defaults work as-is

docker compose up -d db                # Postgres
docker compose run --rm flyway         # apply db/migrations
docker compose up api web              # http://localhost:5173
```

The API is on `http://localhost:5080`, with its OpenAPI reference at
`http://localhost:5080/scalar` in development.

Registering an account seeds it with the three types from the handoff. Grocery
list and Movie & show list bring their categories; **Default list** is offered
first and has none, so a new list is a plain checklist until you decide otherwise.

### Running the pieces directly

```bash
# API — needs the database up and migrated first
cd backend && dotnet run --project src/Sprout.Api

# Web
cd frontend && npm install && npm run dev
```

### Tests

```bash
cd backend  && dotnet test    # 95 tests
cd frontend && npm test       # 59 tests
```

The backend suite includes tests that run against a real Postgres. They skip
themselves when none is reachable, so `dotnet test` is green either way; start the
database first to actually exercise them.

---

## Layout

```
backend/
  src/Sprout.Domain/          entities, invariants, sorting. No dependencies at all.
  src/Sprout.Application/     MediatR handlers, DTOs, validators, abstractions
  src/Sprout.Infrastructure/  EF Core, Identity, JWT, Google token validation
  src/Sprout.Api/             controllers, auth pipeline, problem details
  tests/                      Domain, Application (incl. Postgres), API end-to-end
db/
  migrations/                 the schema of record, applied by Flyway
  ef-model-snapshot.sql       what EF believes the schema is; see below
frontend/
  src/api/                    typed client, session handling, TanStack Query hooks
  src/screens/                the nine screens from the handoff
  src/components/             design-system primitives
  src/index.css               the Organic tokens as a Tailwind theme
```

Dependencies point inwards only: `Api → Infrastructure → Application → Domain`.
The Domain project has no package references, which is what keeps the sorting and
category rules testable without a database.

---

## Decisions worth knowing

### Flyway owns the schema, EF only maps to it

EF migrations are switched off. Every schema change is a new versioned SQL file in
[`db/migrations`](db/migrations). This keeps one migration story for both the
Identity tables and Sprout's own, and lets the SQL express rules EF cannot:

- category and type names unique **case-insensitively**, via `lower(name)` indexes
- partial unique indexes on `list_members`, where half the rows have a null
  `user_id` and half a null `invited_email`
- one owner per list, as a partial unique index on `role = 0`
- `todo_items.category_id` referencing `categories` with `ON DELETE RESTRICT`,
  which is what forces the delete-category path to clear its items first

The risk with this split is silent drift, so two tests guard it:

- **`EfModelSnapshotTests`** pins the DDL EF believes in to
  `db/ef-model-snapshot.sql`. Change a mapping and it fails, telling you to write
  the migration.
- **`PostgresSchemaTests`** writes and reads every table against a real Postgres,
  which is the only thing that catches a genuine column or type mismatch.

### ASP.NET Identity rather than Neon Auth

Identity lives in the API, so the user table, the token lifetime and the sign-in
flows are all in the codebase and testable offline. The trade is that refresh,
reset and confirmation flows are ours to build.

The seam is kept narrow deliberately: the Application layer depends on
`ICurrentUser` and `IIdentityService`, never on Identity or `HttpContext`.
Swapping to Neon Auth (or any JWKS issuer) means reimplementing those two
interfaces and changing the bearer configuration, with no handler touched.

### Tokens

The access token is short-lived and held **in memory only**, so a script that can
read `localStorage` cannot lift it. The refresh token is persisted, because
staying signed in across a reload is the point, and is mitigated instead:
stored only as a SHA-256 hash, single-use, and rotated on every exchange with the
replacement recorded, so reuse is detectable. Concurrent 401s share one in-flight
refresh, so a page with several queries never spends the token twice.

### No realtime in v1

Item completion is shared state, but there is no SignalR hub yet. TanStack Query
refetches on window focus with a 10-second stale time, toggles apply optimistically
and roll back on failure, and an offline banner says when the list has stopped
being live. Adding a hub later means invalidating the same query keys.

### Sorting happens on the server

The API returns items already ordered for the caller's chosen sort, open ones
first and completed ones last. The client only decides where category headers go.
That keeps the four sort comparators, including the type's custom category order,
in one tested place rather than duplicated in TypeScript.

### Categories are optional

An item's `category_id` is nullable and a type may have none at all, which is how
the seeded **Default list** ships: a plain checklist. The handoff faked this with a
catch-all category named "Uncategorised" plus a rule that hid the chrome whenever
every item sat in it. That name carried behaviour, which meant renaming a category
could silently change how a list rendered. It is retired: there is now one way to
be uncategorised instead of two.

A list renders with no category chrome, `isPlain`, when nothing on it sits in a
category the type still has. That covers an empty list, a list of loose items, and
one whose categories were deleted. A single filed item brings the grouping back.
Uncategorised items trail the filed ones in a group with no header.

Deleting a category clears its items rather than rehoming them, so nothing is
filed somewhere the user did not choose. The last category can go too.

A row drops its category chip when it sits under a category header, which already
names the group; it keeps the chip under any other sort, and in the completed
section, where no headers are drawn. The checkbox takes the category colour either
way, so a row never loses its tie to its group.

### One default type per account

Registration seeds three types, and one of them is marked `is_default`. That flag,
not creation order, is what puts the default kind at the top of New list and
preselects it. `ListType` cannot see its siblings, so "only one per account" is a
partial unique index on `owner_id` rather than a domain rule.

Categories can be renamed from the type screen. Renaming deliberately does not
reorder, so no list re-groups behind the user; only moving a category does that.

### 404 rather than 403

Asking for a list you are not a member of returns 404. A 403 would confirm the
list exists, which is a membership oracle on a sharing feature.

Once you *are* a member, the reason is safe to give: an editor who tries to rename
or delete the list gets a 403 saying so, because they already know it exists. Only
the owner can delete a list, and the screen hides the control from everyone else
rather than letting the server be the first to say no.

---

## Deploying to a VPS

```bash
cp .env.example .env          # fill in the Neon and JWT values
docker compose -f docker-compose.prod.yml up -d --build
```

Flyway runs against Neon and must succeed before the API starts, so a failed
migration stops the deploy rather than leaving a half-migrated database serving
traffic.

nginx serves the built SPA and proxies `/api` to the API container, so the browser
only ever talks to one origin. The API is not published to the host; nginx is
bound to `127.0.0.1:8080`, expecting the VPS's own reverse proxy to terminate TLS
in front of it.

`VITE_*` values are baked in at build time, which is why they are build arguments
rather than runtime environment. Pointing the app at a different API means
rebuilding the web image.

---

## Open questions from the handoff

Answered here, and worth confirming:

1. **A list cannot override its type's categories.** Implemented as designed.
2. **Renaming and deleting a category** were not designed. Both are implemented:
   deleting moves any items that used the category to the type's catch-all first,
   and the last category of a type cannot be deleted.
3. **Due dates** were displayed but not settable. The Add item sheet now has a
   date field, so the display is not decorative.
4. **The Today tab** is out of scope in the handoff. Rather than ship a dead tab,
   it lists the lists with something still open. Notifications and per-item
   assignees are not built.
5. **A shared list's type** is currently owned and edited by the list owner only;
   members see the categories through the list but cannot reorder them. Worth a
   decision before sharing gets heavier use.

Also not built, and flagged rather than assumed: password reset and email
confirmation (Identity supports both, no flows are wired), and invitation emails —
an invitation is recorded and claimed when that address signs up, but nothing is
sent.
