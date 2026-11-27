# Certificate defaults sample

This host demonstrates the shared Kestrel HTTPS package and standard outbound client registration.

Set the following values through the environment or deployment secret provider before starting:

- ASPNETCORE_URLS=https://0.0.0.0:8443
- ServiceOptions__KestrelOptions__Path to the mounted PFX or PEM full chain
- ServiceOptions__KestrelOptions__Password for PFX only
- ServiceOptions__KestrelOptions__KeyPath for PEM only
- SecurityService__BaseAddress to the HTTPS service URL

For Windows, restrict the certificate file ACL to the service identity. For Docker or another container host, mount the PFX/fullchain and key as read-only secrets and grant the process access. Keep the PFX password in the host's secret provider. After rotating the certificate files, restart this sample to load the new certificate.
