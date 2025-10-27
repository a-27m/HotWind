# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/HotWind.Api/HotWind.Api.csproj", "HotWind.Api/"]
RUN dotnet restore "HotWind.Api/HotWind.Api.csproj"

# Copy source code and build
COPY src/HotWind.Api/ HotWind.Api/
WORKDIR "/src/HotWind.Api"
RUN dotnet build "HotWind.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "HotWind.Api.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final

# Create non-root user
RUN addgroup -g 1000 hotwind && \
    adduser -D -u 1000 -G hotwind hotwind

WORKDIR /app

# Copy published application
COPY --from=publish --chown=hotwind:hotwind /app/publish .

# Switch to non-root user
USER hotwind

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Run the application
ENTRYPOINT ["dotnet", "HotWind.Api.dll"]
