---
id: patch-upgrade
name: Patch/Upgrade Workflow
description: Apply patches and upgrades to Safeguard appliances including clustered environments.
tags: patch, upgrade, update, firmware, maintenance, cluster patch, version
---

ID: patch-upgrade
Name: Patch/Upgrade Workflow
Goal: Safely apply software patches or upgrades to Safeguard appliances, including clustered deployments.

Context: Patches modify the software/configuration of the running appliance. In clustered environments, the patch must be distributed to all members before installation. The appliance enters maintenance mode during installation. Only one cluster operation can run at a time.

IMPORTANT: Always back up before patching. Once installed, patches cannot be uninstalled.

Steps:

## Pre-Patch Checklist
1. Verify current version:
   - GET /v4/ApplianceStatus -> ApplianceVersion, ApplianceState
2. Check cluster health (if clustered):
   - GET /v4/Status/Cluster -> all members should be Online
3. Create a backup:
   - POST /v4/Backups (see backup-restore workflow)
4. Verify no other cluster operations are running:
   - GET /v4/Status/Cluster -> check for lock indicators

## Upload the Patch
5. Upload the patch file:
   - POST /v4/Patches/Upload with multipart form containing the .sgn patch file
   - If verification fails, an error is returned — do not proceed
6. Check uploaded patch details:
   - GET /v4/Patches -> shows uploaded patches, version info, validation status

## Clustered Environment: Distribute First
7. Distribute patch to all cluster members:
   - POST /v4/Patches/{patchId}/Distribute
   - Monitor distribution status: GET /v4/Status/ClusterPatch
   - Wait until all members show distribution complete
8. Run pre-patch checks on all members:
   - POST /v4/Patches/{patchId}/CheckErrors
   - Reviews errors and warnings from every cluster member

## Install the Patch
9. Install (single appliance or after cluster distribution):
   - POST /v4/Patches/{patchId}/Install
   - The appliance enters maintenance mode
   - In a cluster, all members are patched sequentially
10. Monitor installation progress:
    - GET /v4/Status/Maintenance -> shows maintenance state and progress
    - GET /v4/Status/ClusterPatch -> per-member patch status
11. Wait for appliance to return to Online:
    - Poll: GET /v4/Status -> State should return to "Online"
    - This may take several minutes

## Post-Patch Verification
12. Confirm new version:
    - GET /v4/ApplianceStatus -> verify ApplianceVersion matches expected
13. Verify cluster health:
    - GET /v4/Status/Cluster -> all members Online with matching versions
14. Run health check:
    - Use health-check workflow to verify services are functioning

## Remove an Uploaded (Unstaged) Patch
- If you uploaded but haven't installed yet:
  - DELETE /v4/Patches/{patchId}
  - Or: POST /v4/Patches/{patchId}/Remove (removes from all cluster members)

## Troubleshooting
- If patch upload fails: check audit log for PatchUploadFailed
- If timeout errors occur during clustering: wait and retry
- If appliance doesn't return from maintenance: check via Recovery Kiosk or BMC/IPMI
- Do not refresh the browser during patch installation

Related:
- Use backup-restore to create a pre-patch backup.
- Use cluster-operations for cluster state management.
- Use health-check for post-patch verification.
- Use appliance-diagnostics for deep troubleshooting if patching fails.

Source: Official Safeguard administration documentation — Appliance Management, Patch Updates, Patching cluster members.
