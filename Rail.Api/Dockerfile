# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Rail.Api/Rail.Api.csproj", "Rail.Api/"]
COPY ["Rail.Indexing/Rail.Indexing.csproj", "Rail.Indexing/"]
RUN dotnet restore "Rail.Api/Rail.Api.csproj"

# Copy remaining source code
COPY . .
WORKDIR "/src/Rail.Api"

# Build and publish
RUN dotnet build "Rail.Api.csproj" -c Release -o /app/build
RUN dotnet publish "Rail.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published files from build stage
COPY --from=build /app/publish .

# Configure environment variables
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Host=postgres;Database=raildb;Username=postgres;Password=railpass123"

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Rail.Api.dll"]