FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/ClubManagementService/ClubManagementService.API/ClubManagementService.API.csproj", "Services/ClubManagementService/ClubManagementService.API/"]
COPY ["Services/ClubManagementService/ClubManagementService.Application/ClubManagementService.Application.csproj", "Services/ClubManagementService/ClubManagementService.Application/"]
COPY ["Services/ClubManagementService/ClubManagementService.Domain/ClubManagementService.Domain.csproj", "Services/ClubManagementService/ClubManagementService.Domain/"]
COPY ["Services/ClubManagementService/ClubManagementService.Infrastructure/ClubManagementService.Infrastructure.csproj", "Services/ClubManagementService/ClubManagementService.Infrastructure/"]

RUN dotnet restore "Services/ClubManagementService/ClubManagementService.API/ClubManagementService.API.csproj"

COPY . .
WORKDIR "/src/Services/ClubManagementService/ClubManagementService.API"
RUN dotnet build "ClubManagementService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ClubManagementService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClubManagementService.API.dll"]

