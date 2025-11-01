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
"Secrets": {
  "Enabled": true,
  "DefaultProvider": "Infisical",
  "Providers": {
    "Infisical": {
      "HostUri": "https://app.infisical.com",
      "ProjectId": "<project-id>",
      "EnvironmentSlug": "<environment-slug>",
      "SecretPath": "/",
      "ClientId": "<machine-identity-client-id>",
      "ClientSecret": "<machine-identity-client-secret>"
    }
  }
}
```

Environment variables map using double underscores. For example:

```bash
export Infisical__ClientId="..."
export Infisical__ClientSecret="..."
```

## Observability & Logging

No secret values are logged—only the provider state—helping you confirm behaviour without exposing sensitive information.

## Secret Refresh

The first iteration of the integration loads secrets at startup. Restart FlowSynx to consume updates or add secret hot-reload logic in a future enhancement.
