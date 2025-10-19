<div align="center">
  <img src="/img/flowsynx_logo.png" height="120px" alt="FlowSynx Logo" />
  <h2>FlowSynx — Orchestrate Anything. Anywhere.</h2>
  <p><i>Lightweight, extensible, and powerful workflow orchestration for modern automation.</i></p>

  [![Codacy Badge][codacy-badge]][codacy-url]
  [![License: MIT][mit-badge]][mit-url]
  [![Build Status][actions-badge]][actions-url]
  [![FOSSA Status][fossa-badge]][fossa-url]
  [![Good First Issues][github-good-first-issue-badge]][github-good-first-issue-url]
</div>

FlowSynx is a **next-generation workflow orchestration platform** that unifies **automation, scalability, and extensibility** in a single, developer-friendly ecosystem.  
It bridges the gap between **low-code simplicity** and **full-code power**, allowing both developers and operations teams to automate complex tasks seamlessly across **on-prem**, **cloud**, and **hybrid** environments.

Whether you’re streamlining **DevOps**, managing **data pipelines**, or building **enterprise-grade automations**, FlowSynx gives you the control, flexibility, and insight to make it happen.

## Table of Contents

- [What is FlowSynx?](#what-is-flowsynx)
- [How It Works](#how-it-works)
  - [Core Concepts](#core-concepts)
- [Task Lifecycle](#task-lifecycle)
- [Execution Modes](#execution-modes)
- [Runtime Context](#runtime-context)
- [Why FlowSynx?](#why-flowsynx)
- [Key Features](#key-features)
- [Get Started using FlowSynx](#get-started-using-flowsynx)
- [Architecture Overview](#architecture-overview)
  - [Interaction Layers](#interaction-layers)
  - [Core Components](#core-components)
  - [Environments](#environments)
- [User Interfaces](#user-interfaces)
  - [FlowCtl (CLI)](#flowctl-cli)
  - [Web Console](#web-console)
- [Related Repositories](#related-repositories)
- [Community & Contributing](#community--contributing)
- [License](#license)

## What is FlowSynx?

In today’s fast-moving software landscape, organizations demand **repeatable**, **modular**, and **secure** automation — without the lock-in of rigid platforms.

**FlowSynx** redefines orchestration with:
- A **.NET-based micro-kernel engine** built for speed and reliability  
- A **plugin-driven architecture** that evolves with your needs  
- A **JSON-based DAG (Directed Acyclic Graph)** workflow model that’s both human-readable and machine-friendly  

FlowSynx turns your processes — from data management to API and ML/AI automation — into **clear, maintainable, and reusable workflows**.

## How It Works

At its core, FlowSynx executes **DAG-based workflows** where each task represents an atomic operation — reading data, transforming it, sending HTTP requests, or interacting with external systems.

### Core Concepts

- **Workflow JSON** — Define tasks, dependencies, and parameters in simple JSON  
- **Plugins** — Modular building blocks for any functionality (file I/O, APIs, cloud storage, data transformation, ML/AI, etc.)  
- **Execution Engine** — Smart orchestration with:
  - Task dependency resolution (topological sorting)
  - Shared execution context and secure state handling
  - Async execution, retries, and timeouts
  - Built-in logging, metrics, and auditing

## Task Lifecycle

Every task flows through a predictable, reliable lifecycle:

1. **Initialization** – Validates plugin and parameters  
2. **Dependency Wait** – Ensures prerequisite tasks are done  
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

Built on clean architecture principles, FlowSynx offers **clarity**, **control**, and **confidence** — even in the most demanding automation scenarios.

## Key Features

✅ **Plugin-Based Extensibility** — Add, upgrade, or remove capabilities dynamically and version-controlled  
✅ **Cross-Platform Execution** — Runs everywhere (Windows, Linux, macOS, Docker, Cloud)  
✅ **JSON-Defined Workflows** — Declarative, portable, and version-controlled  
✅ **CLI & SDK Support** — Total control for developers and DevOps teams  
✅ **Secure Authentication** — JWT, Basic Auth  
✅ **Triggers & Events** — Webhooks, schedules, and file-change detection  
✅ **Human-in-the-Loop Tasks** — Combine automation with human approval  
✅ **Advanced Logging & Auditing** — Full transparency into every execution  
✅ **Error Handling** - Flexible Error Handling and Retry Policies per task and workflow level  
✅ **Marketplace & Registry** — Discover and manage plugins easily  
✅ **Web Console UI** — Intuitive dashboard for workflow monitoring and control  

## Get Started using FlowSynx

Ready to try FlowSynx? Start automating in minutes.

📘 **Documentation:** [Getting Started Guide](https://flowsynx.io/docs/getting-started)  
🧩 **Samples:** [Example Workflows & Configs](https://github.com/flowsynx/samples)  

## Build from Source

Want to contribute or run FlowSynx locally? Use this quickstart to build from source in minutes.

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) — confirm with `dotnet --version` (should report 9.x)
- Git

### Quickstart

```bash
git clone https://github.com/flowsynx/flowsynx
cd flowsynx
dotnet restore
dotnet build --configuration Release
dotnet test
```

- Commands are cross-platform (Windows, Linux, macOS).
- Build artifacts land in each project's `bin/Release` directory.

For advanced workflows (Docker, local environment setup, contributing guidelines), see [CONTRIBUTING.md](https://github.com/flowsynx/flowsynx/blob/master/CONTRIBUTING.md).

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
Deploy FlowSynx in **local**, **server**, **container**, or **cloud-native** setups — with complete portability.

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

Join a growing community of developers and automation experts.  
You can:
- 💡 Submit ideas and feature requests  
- 🔌 Build and publish plugins  
- 🧱 Contribute to the core or documentation  
- 🌍 Collaborate in discussions  

👉 See [CONTRIBUTING.md](https://github.com/flowsynx/flowsynx/blob/master/CONTRIBUTING.md)

## Communication and Discord

We would greatly appreciate your contributions and suggestions!  
One of the easiest ways to contribute is to participate in Discord discussions.

### Questions and Issues
Reach out with any questions you may have — we’ll make sure to answer them as soon as possible.  
As a community member, feel free to jump in and help others, too!

| Platform | Link |
|-----------|------|
| **Discord (preferred)** | [Discord](https://discord.flowsynx.io) |
| **X (Twitter)** | [@flowsynxio](https://twitter.com/flowsynxio) |

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
