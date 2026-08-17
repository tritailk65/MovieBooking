## About this project

MovieBooking is a microservices-based movie ticket booking system built with .NET 10 and .NET Aspire. It separates catalog, booking, seat reservation, and payment into independent APIs, using PostgreSQL for persistent data, Redis for caching and seat locking, and RabbitMQ for event-driven communication. The solution can be run locally with .NET Aspire or Docker Compose.

Prerequisites:
- Install the latest .NET 10 SDK
- Install Visual Studio Code: https://code.visualstudio.com/
- Clone the eShop repository: https://github.com/tritailk65/MovieBooking.git
- [Install & start Docker Desktop](https://docs.docker.com/engine/install/) 

## Running the solution

Run all commands from the repository root unless a command changes directory explicitly.

### Development with .NET Aspire

Trust the local HTTPS development certificate once:

```shell
dotnet dev-certs https --check --trust
```

Start the Aspire AppHost with its HTTPS launch profile:

```shell
dotnet run --project src/AppHost/AppHost.csproj --launch-profile https
```

The Aspire dashboard displays the external URL assigned to `gateway-api`.

### Development with Docker Compose

Build the images and start the complete local stack:

```shell
docker compose up -d --build
```

Inspect the running services and follow the Gateway logs:

```shell
docker compose ps
docker compose logs -f gateway-api
```

Stop the local stack without deleting its named volumes:

```shell
docker compose down
```

### Build Docker images manually

Docker images are published with the .NET `Release` configuration. The ASP.NET Core
environment (`Development`, `Staging`, or `Production`) is selected when the
container starts, not while the image is built.

```shell
docker build -t moviebooking-catalog:local-staging -f src/Catalog.API/Dockerfile .
docker build -t moviebooking-seat:local-staging -f src/Seat.API/Dockerfile .
docker build -t moviebooking-booking:local-staging -f src/Booking.API/Dockerfile .
docker build -t moviebooking-payment:local-staging -f src/Payment.API/Dockerfile .
docker build -t moviebooking-gateway:local-staging -f src/Gateway.API/Dockerfile .
```

## Testing

### Build and unit tests

```shell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

### Local Docker smoke tests

Start the Development Compose stack before running the tests. Keep the curl
script for a lightweight deployment check:

```shell
chmod +x scripts/smoke-test.sh
./scripts/smoke-test.sh
```


### Integration-test dependencies

Start the isolated PostgreSQL, Redis, and RabbitMQ test dependencies:

```shell
docker compose -f tests/Booking.Saga.IntegrationTests/docker-compose.integration.yml -p moviebooking-integration up -d --wait
dotnet test tests/Booking.Saga.IntegrationTests/Booking.Saga.IntegrationTests.csproj
docker compose -f tests/Booking.Saga.IntegrationTests/docker-compose.integration.yml -p moviebooking-integration down -v
```
