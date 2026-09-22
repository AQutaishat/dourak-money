#!/usr/bin/env bash
# Backs up the production Postgres database and evidence-uploads volume.
# Runs ON the Oracle server (via cron and via deploy.yml, both using this same
# script so there's one place to change retention/behavior).
#
# Usage: ./scripts/backup-prod.sh
# Output: $BACKUP_DIR/db_<timestamp>.sql.gz and evidence_<timestamp>.tar.gz
#
# Backups live outside the repo checkout (~/backups, not ./backups) so `git pull
# --ff-only` in deploy.yml never touches them and a bad pull/reset can't wipe them.

set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-$HOME/backups}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"

mkdir -p "$BACKUP_DIR"

echo "[backup-prod] Dumping Postgres..."
docker exec dourak-postgres pg_dump -U dourak dourak | gzip > "$BACKUP_DIR/db_${TIMESTAMP}.sql.gz"

echo "[backup-prod] Archiving evidence volume..."
docker run --rm \
  -v dourak-evidence-data:/data:ro \
  -v "$BACKUP_DIR":/backup \
  alpine tar czf "/backup/evidence_${TIMESTAMP}.tar.gz" -C /data .

echo "[backup-prod] Pruning backups older than ${RETENTION_DAYS} days..."
find "$BACKUP_DIR" -maxdepth 1 -name 'db_*.sql.gz' -mtime +"$RETENTION_DAYS" -delete
find "$BACKUP_DIR" -maxdepth 1 -name 'evidence_*.tar.gz' -mtime +"$RETENTION_DAYS" -delete

echo "[backup-prod] Done: $BACKUP_DIR/db_${TIMESTAMP}.sql.gz, $BACKUP_DIR/evidence_${TIMESTAMP}.tar.gz"
