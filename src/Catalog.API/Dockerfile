# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props NuGet.config ./
COPY src/Catalog.API/Catalog.API.csproj src/Catalog.API/
COPY src/EventBus/EventBus.csproj src/EventBus/
COPY src/EventBusRabbitMq/EventBusRabbitMq.csproj src/EventBusRabbitMq/
COPY src/IntergrationEventLogEF/IntergrationEventLogEF.csproj src/IntergrationEventLogEF/
COPY src/ServiceDefaults/ServiceDefaults.csproj src/ServiceDefaults/

RUN dotnet restore src/Catalog.API/Catalog.API.csproj

COPY . .
RUN dotnet publish src/Catalog.API/Catalog.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
