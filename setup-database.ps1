# Database Setup and Migration Script

Write-Host "🗄️ Expense Tracker - Database Setup" -ForegroundColor Green
Write-Host ""

# Check if Docker is running
$dockerRunning = docker ps 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️ Docker is not running. Please start Docker Desktop." -ForegroundColor Yellow
    Write-Host "Or install PostgreSQL manually and update connection string in local.settings.json" -ForegroundColor Yellow
    exit
}

# Start PostgreSQL using Docker Compose
Write-Host "🐳 Starting PostgreSQL container..." -ForegroundColor Cyan
docker-compose up -d postgres

# Wait for PostgreSQL to be ready
Write-Host "⏳ Waiting for PostgreSQL to be ready..." -ForegroundColor Cyan
Start-Sleep -Seconds 5

# Check if EF Core tools are installed
Write-Host "🔧 Checking EF Core tools..." -ForegroundColor Cyan
$efInstalled = dotnet tool list -g | Select-String "dotnet-ef"
if (-not $efInstalled) {
    Write-Host "📦 Installing EF Core tools..." -ForegroundColor Cyan
    dotnet tool install --global dotnet-ef
}

# Update local.settings.json if it doesn't exist
if (-not (Test-Path "local.settings.json")) {
    Write-Host "📝 Creating local.settings.json..." -ForegroundColor Cyan
    Copy-Item "local.settings.sample.json" "local.settings.json"
    
    # Update connection string for Docker
    $settings = Get-Content "local.settings.json" | ConvertFrom-Json
    $settings.Values.PostgreSqlConnection = "Host=localhost;Database=ExpenseTrackerDb;Username=postgres;Password=postgres123"
    $settings | ConvertTo-Json -Depth 10 | Set-Content "local.settings.json"
}

# Create migration
Write-Host "📋 Creating database migration..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

# Apply migration
Write-Host "🚀 Applying database migration..." -ForegroundColor Cyan
dotnet ef database update

Write-Host ""
Write-Host "✅ Database setup completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Database Connection Details:" -ForegroundColor Yellow
Write-Host "  Host: localhost"
Write-Host "  Port: 5432"
Write-Host "  Database: ExpenseTrackerDb"
Write-Host "  Username: postgres"
Write-Host "  Password: postgres123"
Write-Host ""
Write-Host "🌐 pgAdmin (Database Management):" -ForegroundColor Yellow
Write-Host "  URL: http://localhost:5050"
Write-Host "  Email: admin@expensetracker.com"
Write-Host "  Password: admin123"
Write-Host ""
Write-Host "▶️ Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Run 'func start' or 'dotnet run' to start the function app"
Write-Host "  2. Test the API using test-api.http file"
Write-Host "  3. Access pgAdmin at http://localhost:5050 to view data"
