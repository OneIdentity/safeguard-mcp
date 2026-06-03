# Build stage — full SDK plus AOT toolchain
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
RUN apt-get update && apt-get install -y --no-install-recommends \
    clang zlib1g-dev \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY src/SafeguardMcp/SafeguardMcp.csproj ./SafeguardMcp/
RUN dotnet restore SafeguardMcp/SafeguardMcp.csproj -r linux-x64
COPY src/SafeguardMcp/ ./SafeguardMcp/
RUN dotnet publish SafeguardMcp/SafeguardMcp.csproj \
    --no-restore \
    -c Release -r linux-x64 -p:PublishAot=true \
    -o /app/publish

# Runtime stage — no .NET runtime, just native deps.
#
# The chiseled base image already declares a built-in nonroot user `app`
# at $APP_UID (= 64198 on Ubuntu Noble chiseled images). We use that
# canonical Microsoft user rather than forcing a Google-distroless-style
# UID 65532, which would require shell-less /etc/passwd manipulation in
# an intermediate stage.
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish/SafeguardMcp .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Default args: --http so the container exposes the streamable-HTTP MCP
# transport on port 8080. Container deployments typically serve a team
# rather than one local user; for stdio use, prefer the npm package
# (`npx @oneidentity/safeguard-mcp`) or the standalone binary.
ENTRYPOINT ["./SafeguardMcp", "--http"]
