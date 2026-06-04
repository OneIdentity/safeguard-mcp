---
id: a2a-credential-retrieval
name: A2A Credential Retrieval Setup
description: Configure Application-to-Application credential retrieval for automated systems.
tags: a2a, application, credential, retrieval, certificate, api key, automation
---

ID: a2a-credential-retrieval
Name: A2A Credential Retrieval Setup
Goal: Configure Application-to-Application (A2A) credential retrieval so automated systems can fetch passwords without human interaction.

Prerequisites:
- A client certificate (with private key) for the application
- The certificate's signing CA uploaded to Safeguard trusted certificates
- A certificate user created and mapped to the client certificate thumbprint
- The target managed accounts must already exist

Steps:
1. Upload the trusted CA certificate (if not already present):
   - POST /v4/TrustedCertificates with the CA certificate (base64 PEM or PFX)
2. Create a certificate user:
   - POST /v4/Users with body:
     {
       "PrimaryAuthenticationProvider": {"Id": -2},
       "UserName": "MyApp-A2A",
       "Thumbprint": "<client-cert-thumbprint>",
       "AdminRoles": []
     }
   - Note: PrimaryAuthenticationProvider Id -2 is the Certificate provider
3. Create the A2A registration:
   - POST /v4/A2ARegistrations with body:
     {
       "AppName": "MyAutomatedApp",
       "Description": "Credential retrieval for CI/CD pipeline",
       "CertificateUserId": <userId>,
       "VisibleToCertificateUser": true
     }
4. Add credential retrievals (one per account):
   - POST /v4/A2ARegistrations/{registrationId}/RetrievableAccounts with body:
     {
       "AccountId": <accountId>,
       "IpRestrictions": ["10.5.0.0/16"]
     }
   - Each retrieval generates a unique API key
5. Retrieve the API key for use by the application:
   - GET /v4/A2ARegistrations/{registrationId}/RetrievableAccounts
   - The ApiKey field in the response is what the application uses
6. Test credential retrieval (from the application):
   - The application calls the A2A service endpoint with:
     - Client certificate authentication (mutual TLS)
     - Authorization header: A2A <apiKey>
     - GET https://<appliance>/service/core/v4/A2ARegistrations/{registrationId}/RetrievableAccounts/{accountId}/Password

Optional - Access Request Broker:
- POST /v4/A2ARegistrations/{registrationId}/AccessRequestBroker/Add
- Allows the application to create access requests on behalf of users

Optional - Event Listeners:
- A2A supports SignalR event listeners for password/SSH key rotation notifications
- Use PersistentSafeguardA2AEventListener (SafeguardDotNet) for production
- Fires when managed passwords or SSH keys are rotated

IP Restrictions:
- Restrict which IPs can call the A2A service per credential retrieval
- Notation: IPv4/IPv6 addresses or CIDR ranges (e.g., 10.5.0.0/16)

Related:
- Use password-rotation-status to verify managed accounts are rotating properly.
- Use certificate-management for CA and client certificate operations.

Source: Official Safeguard administration documentation — Security Policy Management, Application to Application.
