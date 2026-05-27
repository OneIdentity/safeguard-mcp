FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SafeguardMcp/SafeguardMcp.csproj ./SafeguardMcp/
RUN dotnet restore SafeguardMcp/SafeguardMcp.csproj

COPY src/SafeguardMcp/ ./SafeguardMcp/
RUN dotnet publish SafeguardMcp/SafeguardMcp.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SafeguardMcp.dll", "--http"]
