# erp-office-report (Telerik Reporting REST)

ASP.NET Core host for `.trdp` report definitions used by **erp-office**.

## Telerik license (required locally)

Do **not** commit `telerik-license.txt` to GitHub. It contains your Telerik license JWT.

1. Copy `telerik-license.txt.example` → `telerik-license.txt`
2. Paste your license key from [Telerik Your Licenses](https://www.telerik.com/account/your-licenses) (one JWT line only; remove the comment lines from the example if you keep them in the real file)
3. Build/run — `Telerik.Licensing` picks up `telerik-license.txt` in the project root

On a server, deploy `telerik-license.txt` via secure configuration (not from the git repo).

## Configuration

1. Copy `appsettings.example.json` → `appsettings.json` and set JWT secret + SQL connection strings.
2. `appsettings.json` is **gitignored** — never commit real secrets.
3. `regCompany.json` maps `X-Reg-Id` headers to connection string names (safe to commit; no passwords).
4. Optional: override via environment variables or `appsettings.Development.json` (logging only).

## GitHub / deploy checklist

| Push | Do not push |
|------|-------------|
| Source, `erpofficereport.csproj`, `erpofficereport.sln`, `Reports/**` (.trdp, images) | `telerik-license.txt` |
| `appsettings.example.json`, `regCompany.json` | `appsettings.json` |
| `README.md`, `.gitignore`, `telerik-license.txt.example` | `bin/`, `obj/`, `.vs/`, `*.user`, `*.rar` |
