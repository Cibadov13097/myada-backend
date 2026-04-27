# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
RUN mkdir -p /app/logs && chown -R $APP_UID /app/logs
USER $APP_UID
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/NotificationService/NotificationService.Api/NotificationService.Api.csproj", "Services/NotificationService/NotificationService.Api/"]
COPY ["Services/NotificationService/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "Services/NotificationService/NotificationService.Infrastructure/"]
COPY ["Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["Services/NotificationService/NotificationService.Application/NotificationService.Application.csproj", "Services/NotificationService/NotificationService.Application/"]
COPY ["Services/NotificationService/NotificationService.Domain/NotificationService.Domain.csproj", "Services/NotificationService/NotificationService.Domain/"]
RUN dotnet restore "./Services/NotificationService/NotificationService.Api/NotificationService.Api.csproj"
COPY . .
WORKDIR "/src/Services/NotificationService/NotificationService.Api"
RUN dotnet build "./NotificationService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NotificationService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]
