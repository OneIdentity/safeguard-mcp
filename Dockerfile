# syntax=docker/dockerfile:1
#
# Single-stage runtime image. AOT binaries are built outside Docker
# (in dedicated per-RID pipeline jobs) and copied in via buildx's
# TARGETARCH so a single Dockerfile produces both linux/amd64 and
# linux/arm64 variants. Cross-arch dotnet AOT inside `docker buildx`
# is fragile, which is why we don't do it.
#
# Build context layout produced by the Release job:
#   binaries/amd64/SafeguardMcp   (from publish-linux-x64)
#   binaries/arm64/SafeguardMcp   (from publish-linux-arm64)
#
# The chiseled base image already declares a built-in nonroot user
# `app` at $APP_UID (= 1654 on `runtime-deps:10.0-noble-chiseled`,
# verified via `docker buildx imagetools inspect`). We use that
# canonical Microsoft user rather than forcing a Google-distroless
# UID 65532, which would require shell-less /etc/passwd manipulation
# in an intermediate stage.
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled
ARG TARGETARCH
ARG IMAGE_VERSION=0.0.0-dev

LABEL org.opencontainers.image.source="https://github.com/OneIdentity/safeguard-mcp" \
      org.opencontainers.image.description="Safeguard MCP Server" \
      org.opencontainers.image.licenses="Apache-2.0" \
      org.opencontainers.image.version="${IMAGE_VERSION}"

WORKDIR /app
COPY --chmod=0755 --chown=$APP_UID:$APP_UID binaries/$TARGETARCH/SafeguardMcp .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Default args: --http so the container exposes the streamable-HTTP MCP
# transport on port 8080. Container deployments typically serve a team
# rather than one local user; for stdio use, prefer the npm package
# (`npx @oneidentity/safeguard-mcp`) or the standalone binary.
ENTRYPOINT ["./SafeguardMcp", "--http"]
