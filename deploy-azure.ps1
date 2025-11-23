# Azure Deployment Script for Expense Tracker Functions

# Variables - Update these with your values
$resourceGroup = "ExpenseTrackerRG"
$location = "eastus"
$storageAccount = "expensetrackerstorage$(Get-Random -Maximum 9999)"
$functionApp = "ExpenseTrackerFunctions$(Get-Random -Maximum 9999)"
$postgresServer = "expensetracker-postgres"
$postgresDb = "ExpenseTrackerDb"
$postgresUser = "adminuser"
$postgresPassword = "YourSecurePassword123!"

Write-Host "🚀 Starting Azure deployment..." -ForegroundColor Green

# Login to Azure (uncomment if needed)
# az login

# Create Resource Group
Write-Host "📦 Creating resource group: $resourceGroup" -ForegroundColor Cyan
az group create --name $resourceGroup --location $location

# Create Storage Account
Write-Host "💾 Creating storage account: $storageAccount" -ForegroundColor Cyan
az storage account create `
    --name $storageAccount `
    --resource-group $resourceGroup `
    --location $location `
    --sku Standard_LRS

# Create PostgreSQL Flexible Server
Write-Host "🗄️ Creating PostgreSQL server: $postgresServer" -ForegroundColor Cyan
az postgres flexible-server create `
    --resource-group $resourceGroup `
    --name $postgresServer `
    --location $location `
    --admin-user $postgresUser `
    --admin-password $postgresPassword `
    --sku-name Standard_B1ms `
    --tier Burstable `
    --version 14 `
    --storage-size 32 `
    --public-access 0.0.0.0-255.255.255.255

# Create Database
Write-Host "📊 Creating database: $postgresDb" -ForegroundColor Cyan
az postgres flexible-server db create `
    --resource-group $resourceGroup `
    --server-name $postgresServer `
    --database-name $postgresDb

# Create Function App
Write-Host "⚡ Creating function app: $functionApp" -ForegroundColor Cyan
az functionapp create `
    --resource-group $resourceGroup `
    --consumption-plan-location $location `
    --runtime dotnet-isolated `
    --runtime-version 8 `
    --functions-version 4 `
    --name $functionApp `
    --storage-account $storageAccount `
    --os-type Windows

# Configure Connection String
$connectionString = "Host=$postgresServer.postgres.database.azure.com;Database=$postgresDb;Username=$postgresUser;Password=$postgresPassword;SSL Mode=Require"

Write-Host "🔧 Configuring application settings..." -ForegroundColor Cyan
az functionapp config appsettings set `
    --name $functionApp `
    --resource-group $resourceGroup `
    --settings "PostgreSqlConnection=$connectionString"

# Deploy Function App
Write-Host "📤 Deploying function app..." -ForegroundColor Cyan
func azure functionapp publish $functionApp

Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Deployment Summary:" -ForegroundColor Yellow
Write-Host "  Resource Group: $resourceGroup"
Write-Host "  Function App: $functionApp"
Write-Host "  PostgreSQL Server: $postgresServer.postgres.database.azure.com"
Write-Host "  Database: $postgresDb"
Write-Host ""
Write-Host "🌐 Function App URL: https://$functionApp.azurewebsites.net" -ForegroundColor Green
Write-Host ""
Write-Host "⚠️ Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Run database migrations: dotnet ef database update"
Write-Host "  2. Test the endpoints using the .http file"
Write-Host "  3. Configure firewall rules if needed"
