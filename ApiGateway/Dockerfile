FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["ApiGateway/ApiGateway.csproj", "ApiGateway/"]
COPY ["Shared/Shared.Observability/Shared.Observability.csproj", "Shared/Shared.Observability/"]
RUN dotnet restore "ApiGateway/ApiGateway.csproj"

COPY ["ApiGateway/", "ApiGateway/"]
COPY ["Shared/Shared.Observability/", "Shared/Shared.Observability/"]
WORKDIR /src/ApiGateway
RUN dotnet publish "ApiGateway.csproj" -c Release -o /out --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
