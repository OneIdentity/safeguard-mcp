---
id: health-check
name: Appliance & Cluster Health Assessment
description: Comprehensive health status of the Safeguard deployment.
tags: health, status, cluster, hardware, availability, monitoring
---

ID: health-check
Name: Appliance & Cluster Health Assessment
Goal: Comprehensive health status of the Safeguard deployment.

Steps:
1. Check overall appliance state (no auth required):
   - GET /v4/Status -> State should be "Online"
   - GET /v4/Status/Availability -> All "Is*Available" flags should be true
2. Check hardware and resources:
   - GET /v4/ApplianceStatus/Health
   - Look at CPU, memory, disk in the response
   - Flag: CPU > 80%, Memory > 90%, Disk > 85%
3. Cluster health (if clustered):
   - GET /v4/Status/Cluster -> All members should show State: "Online"
   - Any member disconnected or in Maintenance is a concern
4. Platform task failure count (health indicator):
   - GET /v4/AssetAccounts with query: filter=TaskProperties.HasAccountTaskFailure eq true&count=true
   - If count > 0, use workflow task-triage
5. Recent failed password changes:
   - GET /v4/AuditLog/Passwords/Activities with query: filter=EventName eq 'PasswordChangeFailed'&orderby=-LogTime&limit=20

Thresholds:
- State != Online -> CRITICAL
- Any availability flag false -> WARNING
- Task failures > 10 -> INVESTIGATE
- Cluster member disconnected -> CRITICAL

Related:
- Use task-triage for failing password or SSH key tasks.
- Use appliance-diagnostics when a deeper appliance investigation is needed.

Source: Official Safeguard admin and troubleshooting documentation.
