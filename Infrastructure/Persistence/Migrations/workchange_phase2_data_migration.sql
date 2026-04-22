-- Phase 2 data migration: Remove incompatible WorkChange data
-- Removes negative-changeTime CorrectionEnd entries (old Korrektur Innerhalb)
DELETE FROM work_change WHERE type = 0 AND change_time < 0;
-- Removes old ReplacementStart/End entries that had stored Von/Bis (now replaced by duration-based)
DELETE FROM work_change WHERE type IN (2, 3);
