# TLS certificates

Do not commit certificates or private keys to this repository.

For the reference Docker deployment, provide these files on the deployment host:

```text
deploy/tls/staging/tls.crt
deploy/tls/staging/tls.key
deploy/tls/production/tls.crt
deploy/tls/production/tls.key
```

The certificate must contain the public host configured by `PUBLIC_HOST`. In a
managed cloud environment, prefer the platform's certificate manager and ingress
instead of mounting certificate files into this Nginx container.
