# Infisical Configuration Guide

FlowSynx can load its configuration from [Infisical](https://infisical.com/) in addition to the standard `appsettings*.json` files. This guide walks through the requirements, secret conventions, and runtime controls for switching between providers.

## Prerequisites

1. **Infisical project & environment**  
   Collect the project ID and environment slug that holds your secrets.
2. **Machine identity credentials**  
   Create a machine identity in Infisical and copy its client ID and client secret. The identity should be scoped to the minimal set of secrets FlowSynx needs.
3. **Secret naming**  
   Store each configuration entry as a secret. Use either colon-delimited keys (`Logger:Level`) or double underscores (`Logger__Level`) to map into the hierarchical configuration system.

## Configure FlowSynx

Populate the `Infisical` section (either via `appsettings.json`, user secrets, or environment variables) with your connection metadata:

```json
"Infisical": {
  "Enabled": true,
  "ProjectId": "<project-id>",
  "EnvironmentSlug": "<environment-slug>",
  "SecretPath": "/",
  "HostUri": "https://app.infisical.com",
  "FallbackToAppSettings": true,
  "MachineIdentity": {
    "ClientId": "<machine-identity-client-id>",
    "ClientSecret": "<machine-identity-client-secret>"
  }
}
```

Environment variables map using double underscores. For example:

```bash
export Infisical__MachineIdentity__ClientId="..."
export Infisical__MachineIdentity__ClientSecret="..."
```

> ℹ️ Set `FallbackToAppSettings` to `false` to fail startup instead of silently reverting to JSON when Infisical is unavailable.

## Choose the Configuration Source

FlowSynx selects the configuration source at runtime without recompilation:

- **Command line:** `dotnet run -- --start --config-source=Infisical`
- **Environment variable:** `export FLOWSYNX_CONFIG_SOURCE=Infisical`
- **appsettings:** set `Configuration:Source` to `Infisical` as a default (can still be overridden).

If Infisical is requested but disabled (`Enabled: false`), FlowSynx remains on the JSON files.

## Observability & Logging

During startup FlowSynx logs the active configuration source:

```
info: Configuration source in use: Infisical
```

When fallback is enabled and Infisical cannot be reached, the application logs a warning and continues:

```
warn: Infisical configuration was requested but appsettings.json was used as a fallback.
```

No secret values are logged—only the provider state—helping you confirm behaviour without exposing sensitive information.

## Secret Refresh

The first iteration of the integration loads secrets at startup. Restart FlowSynx to consume updates or add secret hot-reload logic in a future enhancement.
