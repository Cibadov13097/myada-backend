# Build from repository root (see docker-compose: context: ., dockerfile: Services/MyAdaAttendanceService/Dockerfile)
# so Shared/Shared.Observability matches MyAdaAttendanceService.Web.csproj ProjectReference.

# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Shared/Shared.Observability/Shared.Observability.csproj Shared/Shared.Observability/

COPY Services/MyAdaAttendanceService/MyAdaAttendanceService.Web/MyAdaAttendanceService.Web.csproj Services/MyAdaAttendanceService/MyAdaAttendanceService.Web/
COPY Services/MyAdaAttendanceService/MyAdaAttendanceService.Application/MyAdaAttendanceService.Application.csproj Services/MyAdaAttendanceService/MyAdaAttendanceService.Application/
COPY Services/MyAdaAttendanceService/MyAdaAttendanceService.Core/MyAdaAttendanceService.Core.csproj Services/MyAdaAttendanceService/MyAdaAttendanceService.Core/
COPY Services/MyAdaAttendanceService/MyAdaAttendanceService.Infrastructure/MyAdaAttendanceService.Infrastructure.csproj Services/MyAdaAttendanceService/MyAdaAttendanceService.Infrastructure/

RUN dotnet restore Services/MyAdaAttendanceService/MyAdaAttendanceService.Web/MyAdaAttendanceService.Web.csproj

COPY Shared/Shared.Observability/ Shared/Shared.Observability/
COPY Services/MyAdaAttendanceService/ Services/MyAdaAttendanceService/

RUN dotnet publish Services/MyAdaAttendanceService/MyAdaAttendanceService.Web/MyAdaAttendanceService.Web.csproj -c Release -o /app/publish


# -------- RUNTIME STAGE --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MyAdaAttendanceService.Web.dll"]
