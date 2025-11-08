<div align="center">
  <img src="/img/flowsynx_logo.png" height="120px" alt="FlowSynx Logo" />
  <h2>FlowSynx — The Open Automation Fabric</h2>
  <p><i>Orchestrate anything. Anywhere. Powered by plugins. Defined in JSON.</i></p>

  [![Codacy Badge][codacy-badge]][codacy-url]
  [![Quality Gate Status][sonarcloud-quality-gate-badge]][sonarcloud-quality-gate-url]
  [![License: MIT][mit-badge]][mit-url]
  [![Build Status][actions-badge]][actions-url]
  [![FOSSA Status][fossa-badge]][fossa-url]
  [![Good First Issues][github-good-first-issue-badge]][github-good-first-issue-url]
  [![⭐ Star on GitHub](https://img.shields.io/badge/⭐%20Star%20on%20GitHub-555555?style=flat&logo=github)](https://github.com/flowsynx/flowsynx)

  ✨ **Support FlowSynx by giving it a star!** ✨  
  Your support helps others discover the project and drives continued innovation.
</div>

FlowSynx is a **next-generation orchestration platform** designed to unify **automation, scalability, 
and extensibility** within a single, developer-centric ecosystem. It bridges the gap between **low-code simplicity** 
and **full-code power**, enabling teams to automate complex workflows seamlessly across **on-premises**, **cloud**, 
and **hybrid** environments.

Built around **JSON-defined workflows** and a **plugin-driven architecture**, FlowSynx lets you connect any system, 
process, or service — from DevOps pipelines and AI integrations to enterprise data flows. Whether you’re streamlining 
**operations**, managing **data pipelines**, or orchestrating **mission-critical automations**, FlowSynx delivers the 
control, flexibility, and visibility you need to make automation truly universal.

![Demo GIF](/img/Demo.gif)

### 💬 Share FlowSynx

Help grow the community by sharing FlowSynx with your network:

**[Share on X (Twitter)](https://x.com/intent/tweet?text=Check%20out%20this%20awesome%20project%20on%20GitHub!%20⭐%0Ahttps%3A%2F%2Fgithub.com%2Fflowsynx%2Fflowsynx)**  
**[Share on LinkedIn](https://www.linkedin.com/sharing/share-offsite/?url=https%3A%2F%2Fgithub.com%2Fflowsynx%2Fflowsynx)**

Or copy and share this snippet:

```text
⭐ Check out FlowSynx — an open-source automation fabric for orchestrating anything, anywhere:
https://github.com/flowsynx/flowsynx
```

## Table of Contents

- [What is FlowSynx?](#what-is-flowsynx)
- [How It Works](#how-it-works)
  - [Core Concepts](#core-concepts)
- [Task Lifecycle](#task-lifecycle)
- [Execution Modes](#execution-modes)
- [Runtime Context](#runtime-context)
- [Why FlowSynx?](#why-flowsynx)
- [Differentiate & Articulate Uniquely](#differentiate--articulate-uniquely)
- [Key Features](#key-features)
- [Roadmap](#roadmap)
- [Quick Start Experience](#quick-start-experience)
- [Build from Source](#build-from-source)
- [Architecture Overview](#architecture-overview)
  - [Interaction Layers](#interaction-layers)
  - [Core Components](#core-components)
  - [Environments](#environments)
- [User Interfaces](#user-interfaces)
  - [FlowCtl (CLI)](#flowctl-cli)
  - [Web Console](#web-console)
- [Related Repositories](#related-repositories)
- [Community & Contributing](#community--contributing)
- [Security](#security)
- [License](#license)

## What is FlowSynx?

In today’s fast-moving software landscape, teams need **repeatable**, **modular**, and **secure** automation — without being locked into rigid platforms.

**FlowSynx** redefines orchestration with:
- A **.NET-based micro-kernel engine** built for performance and reliability  
- A **plugin-driven architecture** that evolves with your needs  
- A **JSON-based DAG (Directed Acyclic Graph)** workflow model that’s both human-readable and machine-friendly  

FlowSynx transforms your processes — from data management to API and ML/AI automation — into **clear, maintainable, and reusable workflows**.

## How It Works

At its core, FlowSynx executes **DAG-based workflows** where each task represents an atomic operation — reading data, transforming it, sending HTTP requests, or interacting with external systems.

### Core Concepts

- **Workflow JSON** — Define tasks, dependencies, and parameters in simple JSON  
- **Plugins** — Modular building blocks for any functionality (file I/O, APIs, cloud storage, data transformation, ML/AI, etc.)  
- **Execution Engine** — Smart orchestration with:
  - Dependency resolution (topological sorting)
  - Shared execution context and secure state handling
  - Asynchronous execution, retries, and timeouts
  - Built-in logging, metrics, and auditing

## Task Lifecycle

Every task flows through a predictable, reliable lifecycle:

1. **Initialization** – Validates plugin and parameters  
2. **Dependency Wait** – Ensures prerequisite tasks are complete  
3. **Execution** – Runs the plugin logic (e.g., `ReadAsync`, `WriteAsync`)  
4. **Error Handling** – Retries, fallbacks, or workflow failover  
5. **Post-Processing** – Logs, stores outputs, and passes results downstream  

## Execution Modes

Choose how you run FlowSynx — on your terms:

| Mode | Description |
|------|--------------|
| **Standalone** | Lightweight binary for local or embedded use |
| **Dockerized** | Ready for CI/CD pipelines and Kubernetes clusters |
| **API Mode** | Trigger workflows via REST APIs |
| **CLI (`flowctl`)** | Command-line power for developers |

## Runtime Context

- Shared Variables  
- Secure Secrets Management  
- Plugin Buffers & Metadata  
- Real-Time State Tracking  

## Why FlowSynx?

Modern automation is complex — but it doesn’t have to be **complicated**.

FlowSynx is designed to:
- **Empower developers** with modularity and openness  
- **Simplify operations** through declarative configuration  
- **Bridge teams** across development, data, and business processes  
- **Scale effortlessly** across environments  

Built on clean architecture principles, FlowSynx provides **clarity**, **control**, and **confidence** — even in the most demanding automation scenarios.

## Differentiate & Articulate Uniquely

There are countless workflow and orchestration engines on the market — but FlowSynx stands apart by focusing on flexibility, developer empowerment, and true portability.

Here’s what makes FlowSynx unique:

#### 1. NET Native & Plugin-Based

- Built entirely in **.NET**, giving developers seamless integration with the .NET ecosystem.
- **Plugin-first architecture:** add, remove, or update capabilities dynamically. No core modifications required.
- Supports **custom plugins**, enabling anything from cloud storage and APIs to ML/AI tasks.

#### 2. JSON-Defined DAG Workflows

- Workflows are **fully declarative** in JSON — human-readable and machine-friendly.
- DAG execution ensures **reliable dependency handling, asynchronous execution**, and full **observability**.
- Unlike other engines, FlowSynx makes complex workflows **easy to version, maintain, and share**.

#### 3. Human-in-the-Loop Support

- Integrate **approval gates and manual tasks** directly into workflows.
- Automate most of the process while keeping humans in control where needed — ideal for enterprise scenarios.

#### 4. Hybrid, Cross-Platform Execution

- Run **on-premises**, in **containers**, or fully **cloud-native** — no vendor lock-in.
- Lightweight footprint and fast startup for **developer-friendly experimentation**.
- Works everywhere: **Windows, Linux, macOS, Docker, Kubernetes**.

#### 5. Marketplace of Plugins

- Discover, share, and manage pre-built and custom plugins via a central **Marketplace/Registry**.
- Extend capabilities quickly without reinventing the wheel.

#### 6. What Competitors Lack

- Many orchestration tools either:
    - Lock you into a proprietary ecosystem,
    - Force heavyweight deployments, or
    - Lack support for hybrid and human-in-loop workflows.
- FlowSynx solves these gaps **while remaining lightweight, extensible, and fully open-source**.

**Bottom line:** FlowSynx is not just another orchestration engine. It’s a **developer-first, plugin-powered, hybrid automation fabric** that adapts to your environment — not the other way around.

## Key Features

✅ **Plugin-Based Extensibility** — Add, upgrade, or remove capabilities dynamically and version-controlled  
✅ **Cross-Platform Execution** — Runs everywhere (Windows, Linux, macOS, Docker, Cloud)  
✅ **JSON-Defined Workflows** — Declarative, portable, and version-controlled  
✅ **Schema Validation** — Catch errors early with JSON schema checks  
✅ **CLI & SDK Support** — Total control for developers and DevOps teams  
✅ **Robust Authentication & Access Control** — Compatible with JWT and Basic Auth, featuring RBAC  
✅ **Secret Management Integration** — Infisical, Azure Key Vault, HashiCorp Vault, AWS Secrets Manager  
✅ **Triggers & Events** — Webhooks, schedules, and file-change detection  
✅ **Human-in-the-Loop Tasks** — Combine automation with human approval  
✅ **Advanced Logging & Auditing** — Full transparency into every execution  
✅ **Error Handling** - Configurable Error Handling and Retry Policies per task and workflow level  
✅ **Marketplace & Registry** — Discover and manage plugins easily  
✅ **Web Console UI** — Intuitive dashboard for workflow monitoring and control  

## Roadmap

Curious about what’s next? Review the planned milestones in our [Roadmap](./docs/ROADMAP.md).

## Quick Start Experience

Get up and running with FlowSynx in **under 5 minutes** — no complex setup required.

### Option 1 — Run via Docker (Recommended)

If you have Docker installed, you can launch FlowSynx instantly using Docker Compose.

#### 1️⃣ Create a `docker-compose.yml` file

Copy and paste the following:

```yaml
version: '3.8'

services:
  flowsynx:
    image: flowsynx/flowsynx:1.2.3-linux-amd64
    container_name: flowsynx
    environment:
      Security__EnableBasic: true
      Security__BasicUsers__0__Id: 0960a93d-e42b-4987-bc07-7bda806a21c7
      Security__BasicUsers__0__Name: admin
      Security__BasicUsers__0__Password: admin
      Security__BasicUsers__0__Roles__0: admin
      Security__DefaultScheme: Basic
    volumes:
      - flowsynx-data:/app
    working_dir: /app
    ports:
      - "6262:6262"
    command: ["--start"]
    restart: unless-stopped
    networks:
      - basicAuth_net

volumes:
  flowsynx-data:

networks:
  basicAuth_net:
    driver: bridge
```

#### 2️⃣ Start the stack

```bash
docker compose up -d
```

**This will:**
- Start FlowSynx
- Automatically configure admin credentials (admin / admin)
- Expose the FlowSynx API at `http://localhost:6262` (local-only access)

Verify it's running:

```bash
curl http://localhost:6262/version
```

### Option 2 — Use a Pre-Built Binary

Prefer to run FlowSynx locally?  
Download a pre-built binary for your OS from the latest release:

👉 [**Download FlowSynx Releases**](https://github.com/flowsynx/flowsynx/releases/latest)

#### Prerequisites
- Update your appsettings.json to define the users for Basic authentication mode, for example:

```json
{
  "Security": {
    "EnableBasic": true,
    "BasicUsers": [
      {
        "id": "0960a93d-e42b-4987-bc07-7bda806a21c7",
        "name": "admin",
        "password": "admin",
        "roles": [ "admin" ]
      }
    ],
    "DefaultScheme": "Basic"
  }
}
```

Then run:

```bash
flowsynx --start
```

## Build from Source

Want to contribute or customize FlowSynx?  
You can build it locally in just a few commands.

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) — verify with `dotnet --version` (should report 9.x)
- Git

### Local Build

```bash
git clone https://github.com/flowsynx/flowsynx
cd flowsynx
dotnet restore
dotnet build --configuration Release
dotnet test
```

- Works seamlessly on **Windows**, **Linux**, and **macOS**.  
- Build outputs are placed in each project’s `bin/Release` directory.


## Hello Workflow Example
Once FlowSynx is running (via Docker or binary), try creating and executing your first workflow using the REST API.

### 1️⃣ Add a Workflow
Call this api:

```bash
curl -u admin:admin -X POST http://localhost:6262/workflows \
  -H "Content-Type: application/json" \
  -d '
  {
      "name": "Hello Workflow",
      "description": "A minimal FlowSynx workflow example",
      "tasks": [
        {
          "name": "print_hello",
          "type": "",
          "parameters": {
            "operation": "write",
            "path": "results/test.txt",
            "data": "Hello, FlowSynx!",
            "overwrite": false
          }
        }
      ]
    }
  '
```

### Example response
```json
{
  "data": {
    "id": "<UUID>",
    "name": "Hello Workflow"
  },
  "messages": [
    "The workflow has been added successfully."
  ],
  "succeeded": true,
  "generatedAtUtc": "2025-11-04T14:40:27Z"
}
```
📘 Note: Keep the <UUID> value — you’ll need it to run the workflow.

### 2️⃣ Execute the Workflow
```bash
curl -u admin:admin -X POST http://localhost:6262/workflows/<UUID>/executions
```

###  3️⃣ Verify Output
After execution, check the file `results/test.txt`
inside the FlowSynx container or on your host system — you should see:

```
Hello, FlowSynx!
```

For advanced workflows, Docker setup:

📘 **Documentation:** [Getting Started Guide](https://flowsynx.io/docs/getting-started)  
🧩 **Samples:** [Example Workflows & Configs](https://github.com/flowsynx/samples)

## Architecture Overview

<img src="/img/architecture-diagram.jpg" alt="FlowSynx Architecture Diagram"/>

### Interaction Layers
- **CLI (FlowCtl)** — Lightweight command-line orchestration  
- **REST API Gateway** — Secure, API-first automation  
- **SDKs & Libraries** — Integrate FlowSynx into your own apps  

### Core Components
- **Workflow Orchestrator** — Executes and manages JSON-defined DAGs  
- **Plugin Manager** — Loads and maintains plugins dynamically  
- **Security & Auth Layer** — Ensures safe access and execution  
- **Logging & Auditing Engine** — Observability built in  
- **Trigger Engine** — React to events, schedules, and external signals  

### Environments
Deploy FlowSynx locally, in containers, or cloud-native — with complete portability.

## User Interfaces

### FlowCtl (CLI)
Powerful, scriptable, and developer-friendly.

![Flowctl CLI Screenshot](/img/flowctl.jpg)

### Web Console
A clean, interactive dashboard for:
- Workflow management  
- Real-time monitoring  
- Execution logs and metrics  
- Human task approvals  

![FlowSynx Web Console Screenshot](/img/console.png)

## Related Repositories

| Repository | Description |
|-------------|-------------|
| [**FlowSynx**](https://github.com/flowsynx/flowsynx) | Core engine and runtime for executing JSON-based DAG workflows with plugin-based orchestration. |
| [**FlowCtl**](https://github.com/flowsynx/flowctl) | Command-line tool for initializing, running, and managing FlowSynx workflows. |
| [**Console**](https://github.com/flowsynx/console) | Web-based dashboard for managing, monitoring, and visualizing workflows. |
| [**Docs**](https://flowsynx.io/docs/overview) | Official documentation with setup guides, examples, and API references. |
| [**Samples**](https://github.com/flowsynx/samples) | Ready-to-run example workflows and configuration templates. |
| [**Plugin Core**](https://github.com/flowsynx/plugin-core) | SDK and interfaces for building custom FlowSynx plugins. |
| [**FlowPack**](https://github.com/flowsynx/flowpack) | CLI tool to package and publish FlowSynx plugins as `.fspack` bundles. |
| [**C# SDK**](https://github.com/flowsynx/csharp-sdk) | .NET SDK for integrating and controlling FlowSynx programmatically. |
| [**Plugin Marketplace**](https://github.com/flowsynx/plugin-registry) | Central hub to discover, publish, and manage FlowSynx plugins. |
| [**Plugin Template**](https://github.com/flowsynx/plugin-template-project) | .NET project template for creating new FlowSynx plugins quickly. |

## Community & Contributing

Join our growing community of developers and automation experts.  
You can:
- 💡 Submit ideas and feature requests  
- 🔌 Build and publish plugins  
- 🧱 Contribute to the core or documentation  
- 🌍 Collaborate in discussions  
- 🤝 Review the [Code of Conduct](./CODE_OF_CONDUCT.md) to help keep the community welcoming  

👉 See [CONTRIBUTING.md](https://github.com/flowsynx/flowsynx/blob/master/CONTRIBUTING.md)

## Communication and Discord

We’d love your contributions and feedback!
Join our community and discussions on Discord or follow us on X (Twitter):

| Platform | Link |
|-----------|------|
| **Discord (preferred)** | [Discord](https://discord.gg/KJwtjkv7Rj) |
| **X (Twitter)** | [@flowsynxio](https://x.com/flowsynxio) |

## Security

We take the security of FlowSynx seriously.  
If you discover a vulnerability, please review our [Security Policy](./SECURITY.md) for responsible disclosure guidelines.  
Thank you for helping us keep the community safe!

## License

FlowSynx is open-source and licensed under the **MIT License**.  
See [LICENSE](https://github.com/flowsynx/flowsynx/blob/master/LICENSE) for details.

[mit-badge]: https://img.shields.io/github/license/flowsynx/flowsynx?style=flat&label=License&logo=github
[mit-url]: https://github.com/flowsynx/flowsynx/blob/master/LICENSE
[actions-badge]: https://github.com/flowsynx/flowsynx/actions/workflows/flowsynx-release.yml/badge.svg?branch=master
[actions-url]: https://github.com/flowsynx/flowsynx/actions?workflow=flowsynx
[fossa-badge]: https://app.fossa.com/api/projects/git%2Bgithub.com%2Fflowsynx%2Fflowsynx.svg?type=shield&issueType=license
[fossa-url]: https://app.fossa.com/projects/git%2Bgithub.com%2Fflowsynx%2Fflowsynx?ref=badge_shield&issueType=license
[codacy-badge]: https://app.codacy.com/project/badge/Grade/cc8cc16dbade4f5b93b82fd29ed7c879
[codacy-url]: https://app.codacy.com/gh/flowsynx/flowsynx/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade
[github-good-first-issue-badge]: https://img.shields.io/github/issues/flowsynx/flowsynx/good%20first%20issue?style=flat-square&logo=github&label=good%20first%20issues
[github-good-first-issue-url]: https://github.com/flowsynx/flowsynx/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22
[sonarcloud-quality-gate-badge]: https://sonarcloud.io/api/project_badges/measure?project=flowsynx_flowsynx&metric=alert_status
[sonarcloud-quality-gate-url]: https://sonarcloud.io/summary/new_code?id=flowsynx_flowsynx
