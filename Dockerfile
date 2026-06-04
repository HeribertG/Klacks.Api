# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG KLACKS_VERSION_MAJOR=1
ARG KLACKS_VERSION_MINOR=0
ARG KLACKS_VERSION_PATCH=0
ARG KLACKS_BUILD_KEY=local
ARG KLACKS_BUILD_TIMESTAMP=unknown
ARG KLACKS_VARIANT=DEV

# Copy csproj files for restore
COPY Klacks.Api/Klacks.Api.csproj Klacks.Api/
COPY Klacks.Docs/Klacks.Docs.csproj Klacks.Docs/
COPY Klacks.Api.SourceGenerators/Klacks.Api.SourceGenerators.csproj Klacks.Api.SourceGenerators/
COPY Klacks.Plugin.Contracts/Klacks.Plugin.Contracts.csproj Klacks.Plugin.Contracts/
COPY Klacks.Plugin.Messaging/Klacks.Plugin.Messaging.csproj Klacks.Plugin.Messaging/
COPY Klacks.ScheduleOptimizer/Klacks.ScheduleOptimizer.csproj Klacks.ScheduleOptimizer/

# Restore dependencies
RUN dotnet restore Klacks.Api/Klacks.Api.csproj

# Copy everything else
COPY Klacks.Api/ Klacks.Api/
COPY Klacks.Docs/ Klacks.Docs/
COPY Klacks.Api.SourceGenerators/ Klacks.Api.SourceGenerators/
COPY Klacks.Plugin.Contracts/ Klacks.Plugin.Contracts/
COPY Klacks.Plugin.Messaging/ Klacks.Plugin.Messaging/
COPY Klacks.ScheduleOptimizer/ Klacks.ScheduleOptimizer/

# Inject version into constants before build.
# Value-agnostic anchors ([0-9]+ / "[^"]*") so the source default may drift without breaking
# injection. Brittle exact-literal anchors (e.g. "= 0;") would silently no-op once the default
# changes, shipping an image whose baked-in version disagrees with the manifest (update loop).
RUN sed -i -E "s/public const int CMajor = [0-9]+;/public const int CMajor = ${KLACKS_VERSION_MAJOR};/" Klacks.Api/Application/Constants/VersionConstant.cs && \
    sed -i -E "s/public const int CMinor = [0-9]+;/public const int CMinor = ${KLACKS_VERSION_MINOR};/" Klacks.Api/Application/Constants/VersionConstant.cs && \
    sed -i -E "s/public const int CPatch = [0-9]+;/public const int CPatch = ${KLACKS_VERSION_PATCH};/" Klacks.Api/Application/Constants/VersionConstant.cs && \
    sed -i -E "s/public const string CBuildKey = \"[^\"]*\";/public const string CBuildKey = \"${KLACKS_BUILD_KEY}\";/" Klacks.Api/Application/Constants/VersionConstant.cs && \
    sed -i -E "s/public const string CBuildTimestamp = \"[^\"]*\";/public const string CBuildTimestamp = \"${KLACKS_BUILD_TIMESTAMP}\";/" Klacks.Api/Application/Constants/VersionConstant.cs && \
    sed -i -E "s/public static readonly string CVar = \"[^\"]*\";/public static readonly string CVar = \"${KLACKS_VARIANT}\";/" Klacks.Api/Application/Constants/BuildVariantConstant.cs

# Build and publish
RUN dotnet publish Klacks.Api/Klacks.Api.csproj -c Release -o /app/publish

# Runtime stage — pinned version to prevent breaking changes from base image updates
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
WORKDIR /app
EXPOSE 5000
EXPOSE 443

# Native deps:
# - libgssapi-krb5-2: Kerberos for Npgsql
# - libldap2: LDAP authentication
# - curl: healthcheck probe
# - libfontconfig1 / libfreetype6: required by SkiaSharp on Linux so the LLM
#   capability check can rasterize a test bitmap (without these, SkiaSharp's
#   static initializer crashes with "type initializer for SKImageInfo").
# Canonical's primary ports.ubuntu.com IPs are unreachable from Hetzner; use the
# regional de.ports.ubuntu.com mirror (same Canonical infrastructure, different IPs).
RUN if [ -f /etc/apt/sources.list.d/ubuntu.sources ]; then \
        sed -i 's|http://ports.ubuntu.com/ubuntu-ports|http://de.ports.ubuntu.com/ubuntu-ports|g' /etc/apt/sources.list.d/ubuntu.sources; \
    fi && \
    if [ -f /etc/apt/sources.list ]; then \
        sed -i 's|http://ports.ubuntu.com/ubuntu-ports|http://de.ports.ubuntu.com/ubuntu-ports|g' /etc/apt/sources.list; \
    fi && \
    apt-get update && apt-get install -y --no-install-recommends \
        libgssapi-krb5-2 libldap2 curl libfontconfig1 libfreetype6 gosu \
        && rm -rf /var/lib/apt/lists/*

# Create the non-root user. The container itself starts as root so the
# entrypoint can chown volume mounts (Docker named volumes are root:root by
# default and override the Dockerfile's --chown for those paths), then drops
# to appuser via gosu before exec'ing the app.
RUN useradd --no-create-home --shell /bin/false appuser

# Copy published app and assign ownership to appuser in the same step
COPY --chown=appuser:appuser --from=build /app/publish .

# Entrypoint script runs as root, fixes volume permissions, then drops to appuser
COPY --chmod=755 Klacks.Api/deploy/entrypoint.sh /usr/local/bin/entrypoint.sh

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the app via entrypoint (which exec's the app as appuser)
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
