# syntax=docker/dockerfile:1
# ─── Build stage ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first for better Docker layer caching
COPY CertificateApp.csproj ./
RUN dotnet restore CertificateApp.csproj

# Copy the rest of the source and publish
COPY . ./
RUN dotnet publish CertificateApp.csproj -c Release -o /app/publish /p:UseAppHost=false

# ─── Runtime stage ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Render injects PORT (typically 10000 on the free plan) — bind to it.
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "CertificateApp.dll"]
