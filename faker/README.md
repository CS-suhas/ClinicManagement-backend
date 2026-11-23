# Fake data seeder

The seeder inserts demo rows directly into the Oracle tables. Liquibase remains
responsible for creating and updating the schema; it does not load the fake data.

## Docker Compose

Set `ORACLE_PASSWORD` in the repository `.env` file. Start the regular stack
without fake data:

```powershell
docker compose up --build
```

To include the seeder in the development stack:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

This applies schema migrations, runs the seeder, then starts the API. To run
just the development seeder:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml run --rm seeder
```

## Run locally

For an Oracle database whose schema is already up to date:

```powershell
Set-Location faker
npm install
$env:ORACLE_USER = "SYSTEM"
$env:ORACLE_PASSWORD = "your-oracle-password"
$env:ORACLE_CONNECT_STRING = "localhost:1521/FREEPDB1"
npm start
```

The seeder creates five clinics with three providers each, tops up the Patients
table to 100, and adds related demo data once. It marks the run with a dedicated
training clinic name.
