# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props NuGet.config ./
COPY src/Booking.API/Booking.API.csproj src/Booking.API/
COPY src/Booking.Domain/Booking.Domain.csproj src/Booking.Domain/
COPY src/Booking.Infrastructure/Booking.Infrastructure.csproj src/Booking.Infrastructure/
COPY src/EventBus/EventBus.csproj src/EventBus/
COPY src/EventBusRabbitMq/EventBusRabbitMq.csproj src/EventBusRabbitMq/
COPY src/IntergrationEventLogEF/IntergrationEventLogEF.csproj src/IntergrationEventLogEF/
COPY src/ServiceDefaults/ServiceDefaults.csproj src/ServiceDefaults/
COPY src/Seat.API/Seat.API.csproj src/Seat.API/

RUN dotnet restore src/Booking.API/Booking.API.csproj

COPY . .
RUN dotnet publish src/Booking.API/Booking.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Booking.API.dll"]
