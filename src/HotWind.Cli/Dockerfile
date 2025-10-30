# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/HotWind.Cli/HotWind.Cli.csproj", "HotWind.Cli/"]
RUN dotnet restore "HotWind.Cli/HotWind.Cli.csproj"

# Copy source code and publish directly
COPY src/HotWind.Cli/ HotWind.Cli/
WORKDIR "/src/HotWind.Cli"
RUN dotnet publish "HotWind.Cli.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS final

# Create non-root user
RUN addgroup -g 1000 hotwind && \
    adduser -D -u 1000 -G hotwind hotwind

WORKDIR /app

# Copy published application
COPY --from=build --chown=hotwind:hotwind /app/publish .

# Switch to non-root user
USER hotwind

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Run the application
ENTRYPOINT ["dotnet", "HotWind.Cli.dll"]
