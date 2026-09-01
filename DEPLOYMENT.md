# External deployment

This repository contains a .NET 8 Blazor Server application that can be deployed
as a Docker web service on Render or another container host.

## Render setup

Create a Web Service from this repository with:

- Runtime: `Docker`
- Dockerfile path: `./Dockerfile`
- Health check path: `/healthz`
- The service's `PORT` variable is used automatically by the container entrypoint.

Set the database connection using one of these environment variables:

- `DATABASE_URL`: a PostgreSQL URL such as `postgresql://user:password@host:5432/database`
- `ConnectionStrings__DefaultConnection`: a standard Npgsql connection string

`DATABASE_URL` takes precedence when both are present. If neither environment
variable is present, the app falls back to
`ConnectionStrings:DefaultConnection` from the appsettings configuration.

For a new database, optionally set `ADMIN_BOOTSTRAP_PASSWORD` before the first
start to create the initial `admin` administrator. You may also set
`ADMIN_BOOTSTRAP_USERNAME`; it defaults to `admin`. Never commit either value.

The app creates and upgrades its existing tables during startup. Use an external
managed PostgreSQL database for production data; the container filesystem is not
intended for persistent storage.