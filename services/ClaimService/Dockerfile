# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build image with SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy EClaim.Shared project files
COPY EClaim.Shared/EClaim.Shared.csproj ./EClaim.Shared/

# Copy ClaimService project files
COPY ClaimService/ClaimService.csproj ./ClaimService/

# Restore dependencies for ClaimService, including shared library
RUN dotnet restore ClaimService/ClaimService.csproj

# Copy all source files for both ClaimService and EClaim.Shared
COPY EClaim.Shared/ ./EClaim.Shared/
COPY ClaimService/ ./ClaimService/

# Build and publish ClaimService
WORKDIR /src/ClaimService
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Final runtime setup
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClaimService.dll"]