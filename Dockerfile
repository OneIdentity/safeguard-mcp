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

# Runtime stage — no .NET runtime, just native deps
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app/publish/SafeguardMcp .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["./SafeguardMcp", "--http"]
