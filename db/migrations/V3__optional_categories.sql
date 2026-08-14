-- ═══════════════════════════════════════════════════════════════════════════════
-- Sprout — categories become optional
--
-- Until now every item had to sit in a category, and a type had to keep at least
-- one, so "no categories" was faked with a catch-all named "Uncategorised" plus an
-- isPlain rule that hid the chrome. An item can now simply have no category, which
-- makes the catch-all redundant: there is one way to be uncategorised instead of
-- two, and no magic name carrying behaviour.
--
-- The default type ships empty, so the plain checklist is the default experience.
-- ═══════════════════════════════════════════════════════════════════════════════

ALTER TABLE todo_items
    ALTER COLUMN category_id DROP NOT NULL;

-- The foreign key stays ON DELETE RESTRICT. Deleting a category must keep going
-- through the app, which clears its items first; a database-level SET NULL would
-- let a category be dropped from anywhere and silently unfile the items.

-- Release every item that is about to lose its category, in one statement, before
-- the categories themselves go. RESTRICT means the delete below fails otherwise.
UPDATE todo_items
SET category_id = NULL
WHERE category_id IN (
    SELECT c.id
    FROM categories c
    JOIN list_types t ON t.id = c.list_type_id
    WHERE t.is_default OR lower(c.name) = 'uncategorised'
);

-- Two groups go:
--   * every category on the default type, so it becomes the plain checklist it
--     should have been (Errands, House, Food, Admin on accounts seeded earlier)
--   * every catch-all, since the concept is retired. Only the exact seeded name
--     matches, so a category deliberately named something else is untouched.
DELETE FROM categories c
USING list_types t
WHERE t.id = c.list_type_id
  AND (t.is_default OR lower(c.name) = 'uncategorised');
