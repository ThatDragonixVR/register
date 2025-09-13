# Media Scanner Launch Script
Write-Host "Starting Media Scanner..." -ForegroundColor Green
Write-Host ""

# Check for .NET 8
try {
    $dotnetVersion = dotnet --version
    Write-Host "Found .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: .NET 8.0 Runtime not found. Please install .NET 8.0 from https://dotnet.microsoft.com/download" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Check for API key configuration
$metadataServicePath = "Services/MetadataService.cs"
if (Test-Path $metadataServicePath) {
    $content = Get-Content $metadataServicePath -Raw
    if ($content -match "YOUR_OMDB_API_KEY_HERE") {
        Write-Host "WARNING: OMDB API key not configured!" -ForegroundColor Yellow
        Write-Host "Please update the API key in $metadataServicePath" -ForegroundColor Yellow
        Write-Host "Get a free API key from http://www.omdbapi.com/" -ForegroundColor Yellow
        Write-Host ""
    }
}

Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore packages" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Building application..." -ForegroundColor Cyan
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build application" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Launching Media Scanner..." -ForegroundColor Green
Write-Host "The application window should open shortly." -ForegroundColor Green
Write-Host ""

# Run the application
dotnet run

Write-Host ""
Write-Host "Application closed." -ForegroundColor Green
Read-Host "Press Enter to exit"