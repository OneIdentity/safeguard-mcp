---
id: cluster-operations
name: Cluster Join/Unjoin/Failover Operations
description: Manage Safeguard cluster membership including join, unjoin, failover, and recovery.
tags: cluster, join, unjoin, failover, replica, primary, consensus, offline workflow
---

ID: cluster-operations
Name: Cluster Join/Unjoin/Failover Operations
Goal: Manage cluster membership — enroll replicas, remove members, perform failover, and handle lost consensus scenarios.

Context: Safeguard clusters use a consensus model. Only one cluster operation can run at a time (the cluster locks during operations). Operations include: enroll, unjoin, failover, patch, reset, session module join, update IP, and audit log maintenance.

IMPORTANT: These are potentially destructive operations. Always take a backup before cluster changes.

Steps:

## Check Current Cluster State
1. GET /v4/Status/Cluster -> shows all members, their State, and roles
   - States: Online, Offline, Maintenance, StandaloneReadOnly
   - Roles: Primary, Replica

## Enroll a Replica (Join)
2. From the PRIMARY appliance:
   - POST /v4/Cluster/Members with body:
     {
       "Ipv4Address": "<new-replica-ip>",
       "Ipv6Address": null,
       "OperationId": null
     }
   - The new appliance must be in a factory-reset or standalone state
   - Data replicates from primary to the new member
3. Monitor enrollment progress:
   - GET /v4/Status/Cluster -> new member should transition to Online

## Unjoin a Replica
4. Considerations:
   - You can only unjoin REPLICA appliances (not the primary)
   - The remaining cluster must achieve consensus (majority online)
   - The unjoined replica retains its data but enters Read-only mode
5. From any online cluster member:
   - DELETE /v4/Cluster/Members/{memberId}
   - Or: POST /v4/Cluster/Members/{memberId}/Unjoin
6. After unjoin, the appliance is standalone and Read-only
   - To reactivate: POST /v4/Cluster/Members/{memberId}/Activate (on the standalone appliance)

## Failover (Promote Replica to Primary)
7. Use when: you want to intentionally switch which appliance is primary
   - Cluster must have consensus
   - POST /v4/Cluster/Members/{replicaId}/Failover
   - The old primary becomes a replica, the selected replica becomes primary
8. Monitor: GET /v4/Status/Cluster -> verify roles have swapped

## Lost Consensus / Offline Workflow Mode
9. If majority of cluster members are unreachable:
   - The appliance enters "Lost Quorum" or "Isolated" state
   - Enable Offline Workflow Mode to continue operations in isolation:
     POST /v4/Cluster/OfflineWorkflow/Enable
   - Access requests and workflows continue on the isolated appliance
10. Resume online operations (after network restored):
    - POST /v4/Cluster/OfflineWorkflow/Resume
    - Audit logs merge back to the cluster

## Cluster Reset (Last Resort)
11. CAUTION: Only when consensus cannot be restored
    - POST /v4/Cluster/Reset
    - Rebuilds the cluster from this appliance's data
    - Recommendation: Restore from backup rather than reset

## Unlock a Locked Cluster
12. If a cluster operation hangs and the cluster stays locked:
    - Typically resolves automatically; if not, contact support
    - GET /v4/Status/Cluster to check for lock state

Related:
- Use health-check to assess overall cluster health before operations.
- Use backup-restore to create a backup before cluster changes.
- Use patch-upgrade for applying patches across cluster members.

Source: Official Safeguard administration documentation — Cluster Management, Disaster recovery and clusters.
