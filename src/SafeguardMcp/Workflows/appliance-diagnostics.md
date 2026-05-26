---
id: appliance-diagnostics
name: Full Appliance Diagnostic Workflow
description: Collect diagnostic information for appliance troubleshooting.
tags: appliance, diagnostics, support bundle, dns, network, patches
---

ID: appliance-diagnostics
Name: Full Appliance Diagnostic Workflow
Goal: Comprehensive diagnostic information collection for troubleshooting.

Safeguard guidance: You can view appliance information, run diagnostic tests, view and edit network settings, and generate a support bundle.

Steps:
1. Check appliance state:
   - GET /v4/Status/State -> should return "Online"
   - GET /v4/Status/Maintenance -> check if the appliance is in maintenance mode
2. Get appliance information:
   - GET /v4/ApplianceStatus -> version, uptime, appliance ID
3. Check all services:
   - GET /v4/ApplianceStatus/Health -> individual service health
4. Network diagnostics:
   - GET /v4/NetworkInterfaces -> verify IP configuration
   - GET /v4/NetworkDiagnostics/DNS -> DNS resolution tests
5. Check time synchronization:
   - GET /v4/ApplianceStatus/Time -> verify NTP sync
6. Generate support bundle (if needed):
   - POST /v4/DiagnosticPackage -> creates a support bundle for One Identity Support
   - GET /v4/DiagnosticPackage/{id} -> check status or download
7. Check patch status:
   - GET /v4/Patches -> list available and installed patches

Related:
- Use health-check first for a lighter health assessment.
- Use task-triage if diagnostics point to password management failures.

Source: Official Safeguard admin and troubleshooting documentation.
