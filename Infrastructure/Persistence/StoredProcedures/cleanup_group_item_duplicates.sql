-- Idempotent cleanup for group_item table corruption caused by the
-- pre-fix SealedOrder clone-on-every-save bug and the Id-based matching
-- in EntityCollectionUpdateService.UpdateCollection.
--
-- Safe to re-run: deduplicates by (shift_id, group_id) and (client_id, group_id),
-- removes fully orphaned rows, and repairs root pointers on top-level groups.

BEGIN;

DELETE FROM group_item gi
USING group_item gi2
WHERE gi.shift_id IS NOT NULL
  AND gi.shift_id = gi2.shift_id
  AND gi.group_id = gi2.group_id
  AND COALESCE(gi.update_time, gi.create_time, '1970-01-01'::timestamptz) <
      COALESCE(gi2.update_time, gi2.create_time, '1970-01-01'::timestamptz);

DELETE FROM group_item gi
USING group_item gi2
WHERE gi.shift_id IS NOT NULL
  AND gi.shift_id = gi2.shift_id
  AND gi.group_id = gi2.group_id
  AND gi.id < gi2.id;

DELETE FROM group_item gi
USING group_item gi2
WHERE gi.client_id IS NOT NULL
  AND gi.client_id = gi2.client_id
  AND gi.group_id = gi2.group_id
  AND COALESCE(gi.update_time, gi.create_time, '1970-01-01'::timestamptz) <
      COALESCE(gi2.update_time, gi2.create_time, '1970-01-01'::timestamptz);

DELETE FROM group_item gi
USING group_item gi2
WHERE gi.client_id IS NOT NULL
  AND gi.client_id = gi2.client_id
  AND gi.group_id = gi2.group_id
  AND gi.id < gi2.id;

DELETE FROM group_item
WHERE shift_id IS NULL AND client_id IS NULL;

UPDATE "group" SET root = id
WHERE parent IS NULL AND root IS NULL AND lft > 0;

COMMIT;
