# api-report (Telerik Reporting REST - Consolidated)

Unified ASP.NET Core host for `.trdp` report definitions serving both **erp-kendo** (`erpkendoreport`) and **erp-office-portal** (`erpofficereport`).

Merged from separate `erp-kendo-report` and `erp-office-report` projects using advanced report source resolution (RegIdReportSourceResolver).

## Telerik license (required locally)

Do **not** commit `telerik-license.txt` to GitHub. It contains your Telerik license JWT.

1. Copy `telerik-license.txt.example` → `telerik-license.txt`
2. Paste your license key from [Telerik Your Licenses](https://www.telerik.com/account/your-licenses) (one JWT line only; remove the comment lines from the example if you keep them in the real file)
3. Build/run — `Telerik.Licensing` picks up `telerik-license.txt` in the project root

On a server, deploy `telerik-license.txt` via secure configuration (not from the git repo). The project file excludes it from publish output; copy the file manually on the server.

Never run `git add -f telerik-license.txt`.

## Configuration

1. Copy `appsettings.example.json` → `appsettings.json` and set JWT secret + SQL connection strings.
2. `appsettings.json` is **gitignored** — never commit real secrets.
3. `regCompany.json` maps `X-Reg-Id` headers to connection string names (safe to commit; no passwords).
4. The portal sends `X-Reg-Id` on every report REST call; `RegIdReportSourceResolver` applies the matching SQL connection string to each report's `SqlDataSource` at render time.
5. Optional: override via environment variables or `appsettings.Development.json` (logging only).

## Troubleshooting empty reports

Reports show layout but no invoice lines when the **report service database** does not match the **api-core** database for your tenant.

1. **Check which DB the report service uses** (no secrets returned):

   ```http
   GET http://YOUR_REPORT_HOST:90/api/diagnostics/report-connection
   X-Reg-Id: Astar@GEI
   ```

   Response fields: `connectionStringName`, `initialCatalog`, `dataSource`, `regIdFromHeader`.

2. **Compare with portal** — `NEXT_PUBLIC_DEFAULT_REGISTRATION_ID` in `.env` must map (via `regCompany.json`) to the same catalog api-core uses. Example:

   | RegId | Connection name | Typical catalog |
   |-------|-----------------|-----------------|
   | `Astar@123` | `DbConnection` | `AHHA_LIVE` |
   | `Astar@GEI` | `DBConnection_GEI` | `AHHA_GEI2026JUN` |

3. **Browser Network tab** — report REST calls to `/api/reports/` should include header `X-Reg-Id`.

4. **Report service logs** — after rendering, look for: `Report ar/ArInvoice.trdp using connection name ... -> server/catalog`.

5. **Server `appsettings.json`** on the report host must define every name in `regCompany.json` and point to the same SQL instance/catalog as api-core on that environment.

## GitHub / deploy checklist

| Push | Do not push |
|------|-------------|
| Source, `erpkendoreport.csproj`, `erpkendoreport.sln`, `Reports/**` | `telerik-license.txt` |
| `appsettings.example.json`, `regCompany.json` | `appsettings.json` |
| `README.md`, `.gitignore`, `telerik-license.txt.example` | `bin/`, `obj/`, `.vs/`, `*.user`, `*.rar` |

Repository: [lunks41/api-report](https://github.com/lunks41/api-report)
