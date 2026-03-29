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

# Restore dependencies
RUN dotnet restore Klacks.Api/Klacks.Api.csproj

# Copy everything else
COPY Klacks.Api/ Klacks.Api/
COPY Klacks.Docs/ Klacks.Docs/
COPY Klacks.Api.SourceGenerators/ Klacks.Api.SourceGenerators/
COPY Klacks.Plugin.Contracts/ Klacks.Plugin.Contracts/
COPY Klacks.Plugin.Messaging/ Klacks.Plugin.Messaging/

# Inject version into constants before build
RUN sed -i "s/public const int CMajor = 1;/public const int CMajor = ${KLACKS_VERSION_MAJOR};/" Klacks.Api/Application/Constants/VersionConstant.cs && \
    sed -i "s/public const int CMinor = 0;/public const int CMinor = ${KLACKS_VERSION_MINOR};/" Klacks.Api/Application/Constants/VersionConstant.cs && \
    sed -i "s/public const int CPatch = 0;/public const int CPatch = ${KLACKS_VERSION_PATCH};/" Klacks.Api/Application/Constants/VersionConstant.cs && \
    sed -i "s/public const string CBuildKey = \"local\";/public const string CBuildKey = \"${KLACKS_BUILD_KEY}\";/" Klacks.Api/Application/Constants/VersionConstant.cs && \
    sed -i "s/public const string CBuildTimestamp = \"2026-03-03\";/public const string CBuildTimestamp = \"${KLACKS_BUILD_TIMESTAMP}\";/" Klacks.Api/Application/Constants/VersionConstant.cs && \
    sed -i "s/public static readonly string CVar = \"DEV\";/public static readonly string CVar = \"${KLACKS_VARIANT}\";/" Klacks.Api/Application/Constants/BuildVariantConstant.cs

# Build and publish
RUN dotnet publish Klacks.Api/Klacks.Api.csproj -c Release -o /app/publish

# Runtime stage — pinned version to prevent breaking changes from base image updates
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
WORKDIR /app
EXPOSE 5000
EXPOSE 443

# Install Kerberos library required by Npgsql and LDAP library for authentication
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 libldap2 && rm -rf /var/lib/apt/lists/*

# Run as non-root user
RUN useradd --no-create-home --shell /bin/false appuser && chown -R appuser /app
USER appuser

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the app
ENTRYPOINT ["dotnet", "Klacks.Api.dll"]
