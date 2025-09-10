# Rail.Indexing - Train Index Processing Library

A library for parsing, validating and normalizing train indexes with a console utility.

## Project Structure

```
RailIndexing/
├── Rail.Indexing/              # Main library
│   ├── TrainIndex.cs          # Main class for working with indexes
│   └── Rail.Indexing.csproj   # Library project file
├── Rail.Indexing.Tests/        # Unit tests
│   ├── TrainIndexTests.cs     # TrainIndex tests
│   └── Rail.Indexing.Tests.csproj
├── RailCli/                    # Console application
│   ├── Program.cs             # CLI utility
│   └── RailCli.csproj
├── wagons.csv                  # Sample CSV file
└── README.md
```

## Train Index Description

A train index consists of three parts:

1. **Formation station code** (4 or 5 digits; if 5 — the last digit is truncated)
2. **Train number** (2 or 3 digits; normalized to 3 digits with leading zero if needed)
3. **Destination station code** (4 or 5 digits; if 5 — the last digit is truncated)

### Parsing Rules:

- The first part is always the "formation station"
- Parts 2 and 3 can be swapped
- Any separators are supported (space, "-", "/" etc.)
- Normalized format: `XXXX YYY ZZZZ` (example: `7478 035 6980`)

## Installation and Build

### Requirements
- .NET 8.0 SDK or higher

### Build Project

```bash
# Clone repository and navigate to directory
cd RailIndexing

# Restore NuGet packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test Rail.Indexing.Tests
```

### Build CLI Utility

```bash
# Build executable
dotnet build RailCli -c Release

# Publish as single-file executable
dotnet publish RailCli -c Release -r win-x64 --self-contained
```

## Library Usage

### Main Methods

```csharp
using Rail.Indexing;

// Parse index
var trainIndex = TrainIndex.Parse("7478-035-6980");
Console.WriteLine(trainIndex.NormalizedIndex); // 7478 035 6980

// Safe parsing
if (TrainIndex.TryParse("7478/35/6980", out var index))
{
    Console.WriteLine($"Formation station: {index.FormationStationCode}");
    Console.WriteLine($"Train number: {index.TrainNumber}");
    Console.WriteLine($"Destination station: {index.DestinationStationCode}");
}

// Direct normalization
var normalized = TrainIndex.Normalize("7478-35-6980");
Console.WriteLine(normalized); // 7478 035 6980

// Safe normalization
if (TrainIndex.TryNormalize("invalid", out var result))
{
    Console.WriteLine(result);
}
else
{
    Console.WriteLine("Invalid index");
}
```

### Exception Handling

```csharp
try
{
    var index = TrainIndex.Parse("invalid-index");
}
catch (TrainIndexValidationException ex)
{
    Console.WriteLine($"Validation error: {ex.Message}");
}
```

## CLI Utility Usage

### Index Normalization

```bash
# Normalize single index
rail-cli normalize "7478-035-6980"
# Output: 7478 035 6980

# Example with swapped parts
rail-cli normalize "7478/6980/35"
# Output: 7478 035 6980

# Example with 5-digit station codes
rail-cli normalize "74785 035 69801"
# Output: 7478 035 6980
```

### CSV File Statistics

```bash
# Analyze CSV file
rail-cli stats --file wagons.csv
```

Statistics output includes:
- Total number of records
- Number of valid/invalid indexes
- Top formation and destination stations
- Normalization examples
- Invalid index examples

### Help

```bash
rail-cli help
# or
rail-cli --help
```

## Input Data Examples

### Valid Indexes:
- `7478-035-6980` → `7478 035 6980`
- `7478/35/6980` → `7478 035 6980`
- `7478 6980 35` → `7478 035 6980`
- `74785 035 69801` → `7478 035 6980`
- `7478  035   6980` → `7478 035 6980`

### Invalid Indexes:
- `748-35-6980` (station code < 4 digits)
- `7478-35` (insufficient parts)
- `7478-1-6980` (train number < 2 digits)
- `invalid-data` (contains no digits)
- `7478-035-6980-123` (too many parts)

## Testing

The project contains 20+ unit tests covering:

- Valid index parsing
- Various delimiter format handling
- Station code normalization (5th digit truncation)
- Train number normalization (leading zero padding)
- Swapped parts handling
- Validation and exception throwing
- `TryParse` and `TryNormalize` methods
- `Equals`, `ToString`, `GetHashCode` methods

Run tests:

```bash
dotnet test Rail.Indexing.Tests --verbosity normal
```

## CSV Format

The CLI utility expects CSV files with the following characteristics:
- First row may contain headers (skipped)
- Train index should be in the first column
- Quoted values are supported for escaping commas
- Empty rows are ignored

Example `wagons.csv` file:

```csv
TrainIndex,WagonNumber,CargoType,Weight
7478-035-6980,101,Coal,45.5
7478/35/6980,102,Coal,47.2
invalid-index,103,Oil,50.1
```

## Implementation Features

1. **Flexible Parsing**: Automatic detection of train number and destination station order
2. **Robust Validation**: Detailed error messages
3. **Performance**: Minimal allocations, efficient algorithms
4. **Safety**: `TryParse`/`TryNormalize` methods to avoid exceptions
5. **Extensibility**: Easy to add new validation rules

## License

This project is intended for demonstration and educational purposes.