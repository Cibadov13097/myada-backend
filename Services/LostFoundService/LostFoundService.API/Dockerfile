FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/LostFoundService/LostFoundService.Api/LostFoundService.Api.csproj", "Services/LostFoundService/LostFoundService.Api/"]
COPY ["Services/LostFoundService/LostFoundService.Application/LostFoundService.Application.csproj", "Services/LostFoundService/LostFoundService.Application/"]
COPY ["Services/LostFoundService/LostFoundService.Domain/LostFoundService.Domain.csproj", "Services/LostFoundService/LostFoundService.Domain/"]
COPY ["Services/LostFoundService/LostFoundService.Infrastructure/LostFoundService.Infrastructure.csproj", "Services/LostFoundService/LostFoundService.Infrastructure/"]

RUN dotnet restore "Services/LostFoundService/LostFoundService.Api/LostFoundService.Api.csproj"

COPY . .
WORKDIR "/src/Services/LostFoundService/LostFoundService.Api"
RUN dotnet build "LostFoundService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LostFoundService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LostFoundService.Api.dll"]