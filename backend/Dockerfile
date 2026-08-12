# ── Build ─────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Restore against the project files alone, so the layer is cached until a
# dependency actually changes rather than on every source edit.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Sprout.Domain/Sprout.Domain.csproj src/Sprout.Domain/
COPY src/Sprout.Application/Sprout.Application.csproj src/Sprout.Application/
COPY src/Sprout.Infrastructure/Sprout.Infrastructure.csproj src/Sprout.Infrastructure/
COPY src/Sprout.Api/Sprout.Api.csproj src/Sprout.Api/
RUN dotnet restore src/Sprout.Api/Sprout.Api.csproj

COPY src/ src/
RUN dotnet publish src/Sprout.Api/Sprout.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Runtime ───────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# wget backs the container healthcheck; the alpine image does not ship curl.
RUN apk add --no-cache wget icu-libs \
    && addgroup -S sprout && adduser -S sprout -G sprout

# Globalization stays on: the alphabetical sort uses a locale-aware compare.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=1

COPY --from=build --chown=sprout:sprout /app/publish ./

USER sprout
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=3s --start-period=25s --retries=4 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Sprout.Api.dll"]
