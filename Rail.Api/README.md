# Rail API - Train Index Management System

A REST API built with ASP.NET Core for managing train indexes and wagon data, featuring asynchronous CSV processing, caching, and comprehensive data management.

## Features

- **Train Index Processing**: Integration with Rail.Indexing library for parsing and normalizing train indexes
- **Asynchronous CSV Upload**: Background processing of large CSV files with progress tracking
- **Database Support**: SQLite (development) and PostgreSQL (production) support via Entity Framework Core
- **Caching**: Redis-like memory caching for performance optimization (60-second TTL for stats)
- **API Documentation**: Swagger/OpenAPI documentation with interactive testing
- **Validation**: FluentValidation for request validation
- **Logging**: Structured logging with Serilog
- **Health Checks**: Built-in health monitoring
- **Docker Support**: Complete containerization with Docker Compose

## Project Structure

```
RailApi/
├── Controllers/
│   └── TrainsController.cs      # Main API controllers
├── Data/
│   └── RailDbContext.cs         # Entity Framework DbContext
├── Models/
│   ├── Entities.cs              # Database entities (Train, Wagon)
│   └── DTOs.cs                  # Data Transfer Objects
├── Services/
│   ├── TrainService.cs          # Train business logic
│   └── CsvProcessingService.cs  # Background CSV processing
├── Validators/
│   └── Validators.cs            # FluentValidation validators
├── Program.cs                   # Application startup
├── appsettings.json             # Configuration
├── Dockerfile                   # Container definition
└── docker-compose.yml          # Multi-service orchestration
```

## Database Schema

### Train Entity
```csharp
public class Train
{
    public int Id { get; set; }
    public string NormalizedIndex { get; set; }    // Unique, format: "XXXX YYY ZZZZ"
    public DateTime CreatedAt { get; set; }
    public ICollection<Wagon> Wagons { get; set; }
}
```

### Wagon Entity
```csharp
public class Wagon
{
    public int Id { get; set; }
    public string Number { get; set; }
    public bool IsLoaded { get; set; }
    public decimal WeightKg { get; set; }
    public DateTime Date { get; set; }
    public int TrainId { get; set; }               // Foreign key
    public Train Train { get; set; }
}
```

## API Endpoints

### 1. CSV Upload
```http
POST /api/v1/upload/csv
Content-Type: multipart/form-data

Response: 202 Accepted
{
  "jobId": "uuid",
  "status": "Running"
}
```

### 2. Upload Status
```http
GET /api/v1/upload/status/{jobId}

Response: 200 OK
{
  "jobId": "uuid",
  "status": "Completed",
  "progress": 100,
  "startedAt": "2024-01-01T12:00:00Z",
  "completedAt": "2024-01-01T12:05:00Z",
  "result": {
    "processedRecords": 1000,
    "validRecords": 980,
    "invalidRecords": 20
  }
}
```

### 3. Get Trains (with filtering and pagination)
```http
GET /api/v1/trains?page=1&pageSize=20&sortBy=CreatedAt&sortDirection=desc

Query Parameters:
- searchIndex: string (partial match)
- createdAfter: datetime
- createdBefore: datetime
- minWagons: integer
- maxWagons: integer
- minWeight: decimal
- maxWeight: decimal
- sortBy: Id|NormalizedIndex|CreatedAt|WagonsCount|TotalWeight
- sortDirection: asc|desc
- page: integer (default: 1)
- pageSize: integer (default: 20, max: 100)

Response: 200 OK
{
  "items": [...],
  "totalCount": 1000,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 50,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

### 4. Train Statistics (Cached)
```http
GET /api/v1/trains/{normalizedIndex}/stats

Example: GET /api/v1/trains/7478%20035%206980/stats

Response: 200 OK
{
  "normalizedIndex": "7478 035 6980",
  "createdAt": "2024-01-01T12:00:00Z",
  "totalWagons": 25,
  "loadedWagons": 20,
  "emptyWagons": 5,
  "totalWeight": 1250.50,
  "averageWeight": 50.02,
  "maxWeight": 75.00,
  "minWeight": 25.00,
  "earliestWagonDate": "2024-01-01T10:00:00Z",
  "latestWagonDate": "2024-01-01T14:00:00Z",
  "wagons": [...]
}
```

### 5. Health Check
```http
GET /api/v1/health
GET /health

Response: 200 OK
{
  "status": "Healthy",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## CSV File Format

The API expects CSV files with the following columns (case-insensitive):

```csv
TrainIndex,WagonNumber,IsLoaded,WeightKg,Date
7478-035-6980,WAG001,true,45.5,2024-01-01
7478/35/6980,WAG002,false,0,2024-01-01
74785 035 69801,WAG003,true,50.2,2024-01-01T10:30:00
```

**Column Mappings:**
- **TrainIndex**: Raw train index (will be normalized using Rail.Indexing library)
- **WagonNumber**: Unique wagon identifier
- **IsLoaded**: Boolean (true/false, 1/0, yes/no, loaded/empty)
- **WeightKg**: Decimal weight in kilograms
- **Date**: Date/time (various formats supported)

## Setup and Installation

### Prerequisites
- .NET 8.0 SDK
- Docker and Docker Compose (optional)
- PostgreSQL (for production) or SQLite (for development)

### Development Setup

1. **Clone the repository**
```bash
git clone <repository-url>
cd RailApi
```

2. **Restore packages**
```bash
dotnet restore
```

3. **Update database connection** (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=rail.db"
  }
}
```

4. **Run migrations** (if using PostgreSQL)
```bash
dotnet ef database update
```

5. **Start the application**
```bash
dotnet run
```

6. **Open Swagger UI**
Navigate to `https://localhost:7000` (or configured port)

### Docker Setup

1. **Build and run with Docker Compose**
```bash
docker-compose up --build
```

This will start:
- **Rail API** on http://localhost:5000
- **PostgreSQL** on localhost:5432
- **pgAdmin** on http://localhost:5050

2. **Access the services**
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- pgAdmin: http://localhost:5050 (admin@railapi.com / admin123)

## Configuration

### Database Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=rail.db",
    "PostgreSQLConnection": "Host=localhost;Database=raildb;Username=postgres;Password=password"
  }
}
```

### Caching Configuration
```json
{
  "CacheSettings": {
    "TrainStatsExpirationSeconds": 60,
    "DefaultSlidingExpirationSeconds": 30
  }
}
```

### Logging Configuration
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/rail-api-.txt" } }
    ]
  }
}
```

## Caching Strategy

- **Train Statistics**: Cached for 60 seconds with automatic invalidation on data updates
- **Cache Keys**: `train_stats_{normalizedIndex}`
- **Memory Cache**: Uses `IMemoryCache` for high-performance in-memory caching
- **Invalidation**: Automatic cache invalidation when new wagon data is added

## Background Processing

The CSV upload feature uses background processing with:

- **Job Tracking**: Each upload gets a unique job ID
- **Progress Reporting**: Real-time progress updates
- **Error Handling**: Detailed error reporting for invalid records
- **Transaction Safety**: Database transactions ensure data consistency
- **Cancellation Support**: Supports cancellation tokens for graceful shutdown

## Validation

### Train Filter Validation
- Page number must be > 0
- Page size between 1-100
- Valid sort fields and directions
- Date range validation

### CSV File Validation
- File size limit: 10MB
- Supported formats: .csv, .txt
- Content type validation
- Required file validation

## Error Handling

All API endpoints return consistent error responses:

```json
{
  "error": "Validation Failed",
  "message": "Detailed error description",
  "details": ["Specific validation errors"],
  "traceId": "request-trace-id"
}
```

**HTTP Status Codes:**
- 200: Success
- 202: Accepted (async operations)
- 400: Bad Request (validation errors)
- 404: Not Found
- 500: Internal Server Error

## Performance Considerations

- **Database Indexing**: Strategic indexes on frequently queried fields
- **Pagination**: Efficient pagination with skip/take
- **Caching**: Memory caching for expensive operations
- **Async Processing**: Background processing for large file uploads
- **Connection Pooling**: EF Core connection pooling
- **Logging**: Structured logging for performance monitoring

## Testing

### Manual Testing with Swagger
1. Navigate to `/swagger`
2. Use the interactive API documentation
3. Upload sample CSV files
4. Test filtering and pagination

### Sample Requests

**Upload CSV:**
```bash
curl -X POST "https://localhost:7000/api/v1/upload/csv" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@wagons.csv"
```

**Get Trains:**
```bash
curl "https://localhost:7000/api/v1/trains?page=1&pageSize=10&sortBy=CreatedAt"
```

**Get Train Stats:**
```bash
curl "https://localhost:7000/api/v1/trains/7478%20035%206980/stats"
```

## Security Considerations

- **File Upload Limits**: 10MB file size limit
- **Validation**: Comprehensive input validation
- **Error Handling**: Safe error messages (no sensitive data exposure)
- **CORS**: Configurable CORS policies
- **Health Checks**: Monitoring endpoints for system health
- **Logging**: No sensitive data in logs

## Deployment

### Production Deployment
1. Use PostgreSQL as the database
2. Configure proper connection strings
3. Set up proper logging (file-based)
4. Configure HTTPS
5. Set up monitoring and health checks
6. Use Docker for containerization

### Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="Host=postgres;Database=raildb;Username=user;Password=pass"
ASPNETCORE_URLS=https://+:443;http://+:80
```

## Monitoring and Observability

- **Health Checks**: `/health` endpoint
- **Structured Logging**: JSON-structured logs with Serilog
- **Request Logging**: HTTP request/response logging
- **Error Tracking**: Comprehensive error logging with trace IDs
- **Performance Metrics**: EF Core query logging

## License

This project is intended for demonstration and educational purposes.