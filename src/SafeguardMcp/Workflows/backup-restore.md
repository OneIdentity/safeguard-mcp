---
id: backup-restore
name: Backup and Restore Procedures
description: Create appliance backups, manage retention, and restore from backups.
tags: backup, restore, disaster recovery, retention, archive, encryption
---

ID: backup-restore
Name: Backup and Restore Procedures
Goal: Create and manage Safeguard appliance backups, and restore data when needed.

Context: Backups capture all data and security policy configuration. They do NOT include appliance IP address, NTP, or DNS settings. Backups can be encrypted. Restoration from a different appliance requires uploading the backup file first.

CAUTION: If you restore a backup older than the Maximum Password Age in Local Login Control settings, all user accounts (including bootstrap administrator) will be disabled.

Steps:

## Create a Manual Backup
1. Trigger a backup:
   - POST /v4/Backups
   - Returns a backup object with Id, CreatedDate, Size
2. Monitor backup progress:
   - GET /v4/Backups -> list all backups with status
3. Download the backup file:
   - GET /v4/Backups/{backupId}/Download
   - Store securely off-appliance

## Configure Backup Schedule
4. View current backup settings:
   - GET /v4/BackupSettings
5. Set automatic backup schedule:
   - PUT /v4/BackupSettings with body:
     {
       "Enabled": true,
       "ScheduleType": "Daily",
       "ScheduleTime": "02:00",
       "RetentionDays": 30
     }

## Configure Backup Encryption
6. Encrypt backups (recommended for off-appliance storage):
   - PUT /v4/BackupSettings with "EncryptionEnabled": true
   - Encryption password is set during configuration

## Restore from a Backup
7. List available backups:
   - GET /v4/Backups -> select the backup to restore
8. If restoring from a backup taken on a different appliance:
   - Upload the backup file first: POST /v4/Backups/Upload (multipart form)
9. Perform the restore:
   - POST /v4/Backups/{backupId}/Restore
   - The appliance enters maintenance mode during restore
   - Only data is restored; the running software version is NOT changed
   - Cannot restore a backup from a newer version than currently running
10. Verify after restore:
    - GET /v4/ApplianceStatus -> confirm appliance returns to Online
    - Check: NTP, DNS, IP settings are unchanged (verify via Appliance Information)

## Clustered Environment Considerations
- Take backups from the PRIMARY appliance
- To restore a clustered appliance:
  1. Unjoin all replicas
  2. Restore the backup on the standalone (former primary)
  3. Activate the appliance
  4. Re-enroll replicas to rebuild the cluster
- Audit log data from the backup time is retained

## Version Considerations
- Backups from SPP 8.0.0+ can be restored
- Cannot restore a newer-version backup onto an older-version appliance
- The backup version and running version are logged in the Activity Center

## HSM (Hardware Security Module) Warning
- If the backup was created with HSM integration, the same encryption key must still be accessible
- If the key is missing, the appliance will likely Quarantine during restore

Related:
- Use cluster-operations before/after restore in clustered environments.
- Use health-check to verify appliance state after restore.
- Use patch-upgrade if the restored appliance needs updating.

Source: Official Safeguard administration documentation — Backup and Retention, Disaster recovery and clusters.
