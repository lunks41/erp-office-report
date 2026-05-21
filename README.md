# erp-office-report (Telerik Reporting REST)

ASP.NET Core host for `.trdp` report definitions used by **erp-office**.

## Telerik license (required locally)

Do **not** commit `telerik-license.txt` to GitHub. It contains your Telerik license JWT.

1. Copy `telerik-license.txt.example` → `telerik-license.txt`
2. Paste your license key from [Telerik Your Licenses](https://www.telerik.com/account/your-licenses) (one JWT line only; remove the comment lines from the example if you keep them in the real file)
3. Build/run — `Telerik.Licensing` picks up `telerik-license.txt` in the project root

On a server, deploy `telerik-license.txt` via secure configuration (not from the git repo).

## Configuration

- `appsettings.json` — shared defaults (review secrets before pushing)
- Use local overrides (e.g. User Secrets or environment variables) for connection strings and JWT keys in development
