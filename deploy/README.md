# Gateway HTTPS and environment deployment

## Development with Aspire and HTTPS

The AppHost and Gateway both have HTTPS launch profiles. Ensure the ASP.NET Core
development certificate is trusted, then run:

```bash
dotnet dev-certs https --check --trust
dotnet run --project src/AppHost/AppHost.csproj --launch-profile https
```

Use the external HTTPS URL shown for `gateway-api` in the Aspire dashboard as the
Flutter `API_BASE_URL`.

For development with Docker Compose, run:

```bash
docker compose up --build
```

The Docker development URL is `http://localhost:8080`. HTTPS in Docker is reserved
for the deployment stack so local certificates do not need to be copied into an
application container.

## Staging

Create the ignored environment file and replace every placeholder:

```bash
cp deploy/environments/.env.staging.example .env.staging
docker compose \
  --env-file .env.staging \
  -f docker-compose.deploy.yml \
  up -d
```

The expected client URL is:

```text
https://api-staging.moviebooking.example
```

## Production

Create the ignored environment file and replace every placeholder:

```bash
cp deploy/environments/.env.production.example .env.production
docker compose \
  --env-file .env.production \
  -f docker-compose.deploy.yml \
  up -d
```

The expected client URL is:

```text
https://api.moviebooking.example
```

Only Nginx publishes host ports. Gateway, Catalog, Seat, Booking, Payment,
PostgreSQL, Redis and RabbitMQ remain on Docker networks. Nginx redirects HTTP to
HTTPS and terminates TLS; Gateway validates `Host`, trusts only the configured edge
network, and emits HSTS.

Never commit `.env.staging`, `.env.production`, certificates or private keys. Use
the deployment platform's secret manager when one is available.

If the target platform already provides a managed ingress, omit the Nginx service,
terminate TLS at that ingress, and provide its private CIDR through:

```text
ForwardedHeaders__KnownIPNetworks__0
```
