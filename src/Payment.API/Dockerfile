# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props NuGet.config ./
COPY src/Payment.API/Payment.API.csproj src/Payment.API/
COPY src/EventBus/EventBus.csproj src/EventBus/
COPY src/EventBusRabbitMq/EventBusRabbitMq.csproj src/EventBusRabbitMq/
COPY src/ServiceDefaults/ServiceDefaults.csproj src/ServiceDefaults/

RUN dotnet restore src/Payment.API/Payment.API.csproj

COPY . .
RUN dotnet publish src/Payment.API/Payment.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Payment.API.dll"]
