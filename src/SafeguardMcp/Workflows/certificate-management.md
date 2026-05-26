---
id: certificate-management
name: Certificate Management
description: Manage SSL/TLS certificates, trusted CAs, and audit log signing certificates on the appliance.
tags: certificate, SSL, TLS, trusted CA, CSR, audit log signing, thumbprint
---

ID: certificate-management
Name: Certificate Management
Goal: Upload, assign, and manage certificates used by the Safeguard appliance for SSL/TLS, trusted connections, and audit log signing.

Context: Safeguard uses certificates for: HTTPS (SSL/TLS on the appliance web interface), trusting external servers (Trusted CAs), A2A client authentication, audit log signing, syslog client certificates, and directory SSL connections. The default self-signed SSL certificate should be replaced in production.

Steps:

## SSL/TLS Certificates (Appliance HTTPS)
1. List current SSL certificates:
   - GET /v4/SslCertificates -> shows all uploaded SSL certs and which appliance they're assigned to
2. Upload a new SSL certificate (with private key):
   - POST /v4/SslCertificates with multipart form:
     - File: PFX/PKCS12 file containing cert + private key
     - Passphrase: the private key passphrase
3. Create a Certificate Signing Request (CSR):
   - POST /v4/SslCertificates/CSR with body:
     {
       "Subject": "CN=safeguard.company.com",
       "DnsNames": ["safeguard.company.com", "sg.company.com"],
       "IpAddresses": ["10.0.1.50"],
       "KeySize": 2048
     }
   - Returns CSR in PEM format to submit to your CA
4. Install the signed certificate (after CA signs the CSR):
   - POST /v4/SslCertificates/CSR/{csrId}/Install with the signed certificate
5. Assign certificate to appliance(s):
   - POST /v4/SslCertificates/{certId}/AssignToAppliances with body:
     [{"Id": <applianceId>}]
   - Or unassign: POST /v4/SslCertificates/{certId}/UnassignFromAppliances

## Trusted CA Certificates
6. List trusted CAs:
   - GET /v4/TrustedCertificates
7. Upload a trusted CA:
   - POST /v4/TrustedCertificates with the CA certificate file (PEM or DER)
   - Required BEFORE: adding assets with "Verify SSL Certificate" enabled, configuring A2A with client certs, or LDAPS connections
8. Delete a trusted CA:
   - DELETE /v4/TrustedCertificates/{certId}
   - CAUTION: may break connections to assets or directories that depend on this CA

## Audit Log Signing Certificate
9. View current audit log signing certificate:
   - GET /v4/AuditLogSigningCertificate
10. Upload a new audit log signing certificate:
    - POST /v4/AuditLogSigningCertificate with PFX file
    - One per cluster — applies to all members
    - Default is auto-generated, but custom is recommended
11. Create a CSR for audit log signing:
    - POST /v4/AuditLogSigningCertificate/CSR
    - Signs audit log archives with SHA256 + RSA PSS padding

## Certificate Validation Settings
12. Check/set TLS version enforcement:
    - GET /v4/ApplianceStatus/SecureSsl -> whether only TLS 1.2 is used
    - PUT /v4/ApplianceStatus/SecureSsl with body: true (requires restart)
13. Strict CRL checking:
    - GET /v4/ApplianceStatus/StrictCrlChecking
    - PUT /v4/ApplianceStatus/StrictCrlChecking with body: true (requires restart)
    - Enforces CRL/OCSP checking on outbound TLS connections

## Common Certificate Operations
- Find certificate by thumbprint:
  GET /v4/TrustedCertificates with query: filter=Thumbprint eq '<thumbprint>'
- Check expiration dates:
  GET /v4/SslCertificates with query: fields=Subject,Thumbprint,ExpirationDate&orderby=ExpirationDate
- Certificates expiring soon (within 30 days):
  GET /v4/SslCertificates with query: filter=ExpirationDate lt '<date-30-days-out>'

Related:
- Use a2a-credential-retrieval which requires trusted CA certificates for client cert auth.
- Use directory-integration which may need trusted CAs for LDAPS connections.
- Use health-check for overall appliance status including certificate validity.

Source: Official Safeguard administration documentation — Appliance Management, Certificates.
