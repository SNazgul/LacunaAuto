param(
  [string]$ServiceName = "postgresql-x64-17",
  [string]$DatabaseName = "lacunaauto",
  [string]$RoleName = "lacunaauto",
  [string]$RolePassword = "lacunaauto_dev_password",
  [string]$SuperuserPassword = ""
)

$ErrorActionPreference = "Stop"

if ($DatabaseName -notmatch '^[a-zA-Z0-9_]+$') {
  throw "DatabaseName '$DatabaseName' contains unsupported characters."
}

if ($RoleName -notmatch '^[a-zA-Z0-9_]+$') {
  throw "RoleName '$RoleName' contains unsupported characters."
}

$service = Get-Service -Name $ServiceName -ErrorAction Stop
if ($service.Status -ne "Running") {
  Write-Host "Starting PostgreSQL service '$ServiceName'..."
  Start-Service -Name $ServiceName
}

$service.WaitForStatus("Running", [TimeSpan]::FromSeconds(60))
Write-Host "PostgreSQL service '$ServiceName' is running."

$psqlCommand = Get-Command psql -ErrorAction SilentlyContinue
if ($null -eq $psqlCommand) {
  $candidatePaths = @(
    "C:\Program Files\PostgreSQL\17\bin\psql.exe",
    "C:\Program Files\PostgreSQL\16\bin\psql.exe",
    "C:\Program Files\PostgreSQL\15\bin\psql.exe"
  )

  $psqlPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
  if ($null -eq $psqlPath) {
    throw "psql.exe was not found in PATH or known PostgreSQL installation paths."
  }
}
else {
  $psqlPath = $psqlCommand.Source
}

Write-Host "Using psql at: $psqlPath"

$env:PGHOST = "127.0.0.1"
$env:PGPORT = "5432"
$env:PGUSER = "postgres"
if ([string]::IsNullOrEmpty($SuperuserPassword)) {
  Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
else {
  $env:PGPASSWORD = $SuperuserPassword
}

$connected = $false
for ($attempt = 1; $attempt -le 20; $attempt++) {
  & $psqlPath -w -d postgres -c "SELECT 1;" *> $null
  if ($LASTEXITCODE -eq 0) {
    $connected = $true
    break
  }

  Start-Sleep -Seconds 2
}

if (-not $connected) {
  throw "PostgreSQL did not become queryable on 127.0.0.1:5432."
}

$escapedPassword = $RolePassword.Replace("'", "''")
$escapedRoleName = $RoleName.Replace('"', '""')
$escapedDatabaseName = $DatabaseName.Replace('"', '""')

$roleExists = (& $psqlPath -w -At -d postgres -c "SELECT 1 FROM pg_roles WHERE rolname = '$RoleName';").Trim()
if ($LASTEXITCODE -ne 0) {
  throw "Failed to query existing PostgreSQL roles."
}

if ($roleExists -ne "1") {
  & $psqlPath -w -v ON_ERROR_STOP=1 -d postgres -c "CREATE ROLE ""$escapedRoleName"" LOGIN PASSWORD '$escapedPassword';"
  if ($LASTEXITCODE -ne 0) {
    throw "Failed to create PostgreSQL role '$RoleName'."
  }
}
else {
  & $psqlPath -w -v ON_ERROR_STOP=1 -d postgres -c "ALTER ROLE ""$escapedRoleName"" WITH LOGIN PASSWORD '$escapedPassword';"
  if ($LASTEXITCODE -ne 0) {
    throw "Failed to update PostgreSQL role '$RoleName'."
  }
}

$databaseExists = (& $psqlPath -w -At -d postgres -c "SELECT 1 FROM pg_database WHERE datname = '$DatabaseName';").Trim()
if ($LASTEXITCODE -ne 0) {
  throw "Failed to query existing PostgreSQL databases."
}

if ($databaseExists -ne "1") {
  & $psqlPath -w -v ON_ERROR_STOP=1 -d postgres -c "CREATE DATABASE ""$escapedDatabaseName"" OWNER ""$escapedRoleName"";"
  if ($LASTEXITCODE -ne 0) {
    throw "Failed to create PostgreSQL database '$DatabaseName'."
  }
}
else {
  & $psqlPath -w -v ON_ERROR_STOP=1 -d postgres -c "ALTER DATABASE ""$escapedDatabaseName"" OWNER TO ""$escapedRoleName"";"
  if ($LASTEXITCODE -ne 0) {
    throw "Failed to update PostgreSQL database owner for '$DatabaseName'."
  }
}

Write-Host "PostgreSQL bootstrap completed for role '$RoleName' and database '$DatabaseName'."
