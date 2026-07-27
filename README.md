## About this project

MovieBooking is a microservices-based movie ticket booking system built with .NET 10 and .NET Aspire. It separates catalog, booking, seat reservation, and payment into independent APIs, using PostgreSQL for persistent data, Redis for caching and seat locking, and RabbitMQ for event-driven communication. The solution can be run locally with .NET Aspire or Docker Compose.

Prerequisites:
- Install the latest .NET 10 SDK
- Install Visual Studio Code: https://code.visualstudio.com/
- Clone the eShop repository: https://github.com/tritailk65/MovieBooking.git
- [Install & start Docker Desktop](https://docs.docker.com/engine/install/) 

### Running the solution
```shell
cd src/AppHost
dotnet run
```

### Testing flow
Build image
```bat
cd MovieBooking
docker build -t moviebooking-catalog:local -f src/Catalog.API/Dockerfile .
docker build -t moviebooking-booking:local -f src/Booking.API/Dockerfile .
docker build -t moviebooking-seat:local -f src/Seat.API/Dockerfile .
docker build -t moviebooking-payment:local -f src/Payment.API/Dockerfile .
```

Build image and run with docker-compose
```shell
cd MovieBooking
docker compose up -d
```

Check image status 
```shell
docker compose ps
```

Smoke test
```shell
chmod +x scripts/smoke-test.sh
./scripts/smoke-test.sh
```

Happy path smoke test
```shell
chmod +x scripts/smoke-happy-path.sh 
./scripts/smoke-happy-path.sh
```
