<div align="center">
  <img src="/img/flowsynx_logo.png" height="120px" alt="FlowSynx Logo" />
  <h2>FlowSynx — Orchestrate Anything. Anywhere.</h2>
  <p><i>Lightweight, extensible, and powerful workflow orchestration for modern automation.</i></p>

  [![License: MIT][mit-badge]][mit-url]
  [![Build Status][actions-badge]][actions-url]
  [![FOSSA Status][fossa-badge]][fossa-url]
</div>

FlowSynx is a **next-generation workflow orchestration platform** that unifies **automation, scalability, and extensibility** in a single, developer-friendly ecosystem.  
It bridges the gap between **low-code simplicity** and **full-code power**, allowing both developers and operations teams to automate complex tasks seamlessly across **on-prem**, **cloud**, and **hybrid** environments.

Whether you’re streamlining **DevOps**, managing **data pipelines**, or building **enterprise-grade automations**, FlowSynx gives you the control, flexibility, and insight to make it happen.

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
| [**FlowSynx**](https://github.com/flowsynx/flowsynx) | The core FlowSynx engine and runtime responsible for executing JSON-based DAG workflows, managing plugins, handling triggers, and orchestrating task execution across environments. |
| [**FlowCtl**](https://github.com/flowsynx/flowctl) | A lightweight command-line interface (CLI) for initializing, running, validating, and managing FlowSynx workflows directly from the terminal or within CI/CD pipelines. |
| [**Console**](https://github.com/flowsynx/console) | A modern web-based management dashboard for monitoring workflows, visualizing DAG executions, inspecting logs, and performing manual task interventions. |
| [**Docs**](https://flowsynx.io/docs/overview) | The official FlowSynx documentation portal featuring installation guides, workflow configuration examples, plugin development tutorials, and API references. |
| [**Samples**](https://github.com/flowsynx/samples) | A collection of ready-to-run workflow examples and configuration templates showcasing common automation patterns, integrations, and best practices. |
| [**Plugin Core**](https://github.com/flowsynx/plugin-core) | The foundational SDK and interface layer for building custom FlowSynx plugins that extend the engine’s capabilities without modifying its core. |
| [**FlowPack**](https://github.com/flowsynx/flowpack) | A packaging and publishing tool that compiles and distributes FlowSynx plugins into `.fspack` bundles, simplifying deployment and version management. |
| [**C# SDK**](https://github.com/flowsynx/csharp-sdk) | A developer-friendly .NET SDK that enables programmatic control of workflows, execution tracking, and integration with FlowSynx services and APIs. |
| [**Plugin Registry**](https://github.com/flowsynx/plugin-registry) | A central repository and discovery hub where developers can browse, publish, and manage FlowSynx-compatible plugins for different domains and use cases. |
| [**Plugin Template**](https://github.com/flowsynx/plugin-template-project) | A preconfigured .NET Class Library template designed to help developers quickly scaffold and build new FlowSynx plugins following best practices. |

## Community & Contributing

Join a growing community of developers and automation experts.  
You can:
- 💡 Submit ideas and feature requests  
- 🔌 Build and publish plugins  
- 🧱 Contribute to the core or documentation  
- 🌍 Collaborate in discussions  

👉 See [CONTRIBUTING.md](https://github.com/flowsynx/flowsynx/blob/master/CONTRIBUTING.md)

---

## License

FlowSynx is open-source and licensed under the **MIT License**.  
See [LICENSE](https://github.com/flowsynx/flowsynx/blob/master/LICENSE) for details.

---

[mit-badge]: https://img.shields.io/github/license/flowsynx/flowsynx?style=flat&label=License&logo=github
[mit-url]: https://github.com/flowsynx/flowsynx/blob/master/LICENSE
[actions-badge]: https://github.com/flowsynx/flowsynx/actions/workflows/flowsynx-release.yml/badge.svg?branch=master
[actions-url]: https://github.com/flowsynx/flowsynx/actions?workflow=flowsynx
[fossa-badge]: https://app.fossa.com/api/projects/git%2Bgithub.com%2Fflowsynx%2Fflowsynx.svg?type=shield&issueType=license
[fossa-url]: https://app.fossa.com/projects/git%2Bgithub.com%2Fflowsynx%2Fflowsynx?ref=badge_shield&issueType=license
