@echo off
echo Starting Media Scanner...
echo.
echo Make sure you have:
echo 1. .NET 8.0 Runtime installed
echo 2. Updated the OMDB API key in Services/MetadataService.cs
echo.
pause
dotnet restore
if %errorlevel% neq 0 (
    echo Error restoring packages
    pause
    exit /b 1
)

dotnet build
if %errorlevel% neq 0 (
    echo Error building application
    pause
    exit /b 1
)

echo.
echo Starting application...
dotnet run
pause