-- ═══════════════════════════════════════════════════════════════════════════════
-- Sprout — the account's default list type
--
-- The New list screen offered types in creation order, which put the Grocery list
-- first purely because seeding happened to add it first. The catch-all kind is the
-- one wanted when a list is not really a grocery run or a watchlist, so it has to
-- be marked rather than inferred from a timestamp.
-- ═══════════════════════════════════════════════════════════════════════════════

ALTER TABLE list_types
    ADD COLUMN is_default boolean NOT NULL DEFAULT false;

-- Backfill for accounts that registered before this column existed. Matching on
-- the seeded name is honest here and only here: this is a one-off statement about
-- what was seeded in the past, not a rule the running app applies. A renamed type
-- simply keeps no default, and the screen falls back to creation order.
--
-- ix_list_types_owner_id_name is already unique on (owner_id, lower(name)), so at
-- most one row per account can match and the index below cannot be violated.
UPDATE list_types
SET is_default = true
WHERE lower(name) = 'default list';

-- One default per account. ListType cannot see its siblings, so this is the only
-- place the rule can actually be enforced.
CREATE UNIQUE INDEX ix_list_types_one_default
    ON list_types (owner_id)
    WHERE is_default;
