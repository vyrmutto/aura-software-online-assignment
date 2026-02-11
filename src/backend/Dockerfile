# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY ClinicPOS.slnx .
COPY ClinicPOS.Domain/ClinicPOS.Domain.csproj ClinicPOS.Domain/
COPY ClinicPOS.Application/ClinicPOS.Application.csproj ClinicPOS.Application/
COPY ClinicPOS.Infrastructure/ClinicPOS.Infrastructure.csproj ClinicPOS.Infrastructure/
COPY ClinicPOS.API/ClinicPOS.API.csproj ClinicPOS.API/

# Restore only the API project (skips test dependencies)
RUN dotnet restore ClinicPOS.API/ClinicPOS.API.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish ClinicPOS.API/ClinicPOS.API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ClinicPOS.API.dll"]
