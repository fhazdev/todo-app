-- ═══════════════════════════════════════════════════════════════════════════════
-- Sprout — how many of each item
--
-- Items carry a quantity, shown next to the text and adjusted with a stepper on
-- the list. Existing rows are one of the thing, which is what they have always
-- implicitly been.
--
-- Exploratory: this is behind no flag, but the feature is being tried out and may
-- yet be dropped. Reverting means removing the column and its check.
-- ═══════════════════════════════════════════════════════════════════════════════

ALTER TABLE todo_items
    ADD COLUMN quantity integer NOT NULL DEFAULT 1;

-- The floor lives in TodoItem.SetQuantity as well. This is the backstop: a stepper
-- is a UI control, and nothing about it should be the only thing keeping a zero or
-- a negative out of the table.
ALTER TABLE todo_items
    ADD CONSTRAINT ck_todo_items_quantity CHECK (quantity >= 1);
