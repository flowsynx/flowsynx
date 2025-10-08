<div style="text-align: center"><img src="/img/flowsynx_logo.png" height="120px">
<h2>FlowSynx</h2>
</div>

[![License: MIT][mit-badge]][mit-url] [![Build Status][actions-badge]][actions-url] [![FOSSA Status][fossa-badge]][fossa-url]

[mit-badge]: https://img.shields.io/github/license/flowsynx/flowsynx?style=flat&label=License&logo=github
[mit-url]: https://github.com/flowsynx/flowsynx/blob/master/LICENSE
[actions-badge]: https://github.com/flowsynx/flowsynx/actions/workflows/flowsynx-release.yml/badge.svg?branch=master
[actions-url]: https://github.com/flowsynx/flowsynx/actions?workflow=flowsynx
[fossa-badge]: https://app.fossa.com/api/projects/git%2Bgithub.com%2Fflowsynx%2Fflowsynx.svg?type=shield&issueType=license
[fossa-url]: https://app.fossa.com/projects/git%2Bgithub.com%2Fflowsynx%2Fflowsynx?ref=badge_shield&issueType=license

FlowSynx is a lightweight, flexible, plugin-driven, cross-platform workflow orchestration system designed to automate, 
scale, and manage complex file and data workflows across cloud and on-prem environments. It bridges the 
gap between low-code accessibility and full-code extensibility, empowering both developers and operations 
teams to build powerful, declarative workflows in a simple, consistent way.

## What Is FlowSynx?
In today’s rapidly evolving software and data ecosystems, modular orchestration, repeatability, 
and automation are critical for operational success. However, many existing workflow platforms 
suffer from rigidity, overcomplexity, or tight platform coupling.

**FlowSynx** solves these limitations with a developer-centric ecosystem for defining and executing 
**JSON-based DAG workflows**. At its core lies a lightweight **micro-kernel engine** built in .NET, which 
dynamically loads plugins to extend its capabilities without changing the core. This architecture 
allows for scalable, testable, and maintainable systems that can evolve rapidly while staying reliable.

FlowSynx supports a wide range of domains—from DevOps, CI/CD, and data pipelines to industry-specific 
workflows in healthcare, finance, and enterprise automation.

## How It Works
FlowSynx orchestrates workflows as Directed Acyclic Graphs (DAGs), where each node (task) represents 
an atomic operation like reading a file, transforming data, uploading to cloud storage, or triggering 
an HTTP call. These tasks are executed in a controlled, dependency-respecting order, determined by their 
connections in the DAG.

### Core Concepts
- **Workflow JSON**: Workflows are defined using JSON files that describe tasks, dependencies, and parameters. Each task references a plugin and defines its action and input/output.
- **Plugins**: All functionality (e.g., file I/O, API calls, data processing) is handled via dynamically loaded plugins. This enables modularity, versioning, and dynamic extension of the system.
- **Execution Engine**: A core micro-kernel processes the DAG:	
	- Resolves task dependencies via topological sorting
	- Manages shared execution context and state
	- Executes plugins asynchronously with retry and timeout support
	- Captures logs, audit entries, and metrics per task

### Task Lifecycle
Each task follows a lifecycle:
- **Initialization**: Validates plugin/action and parameters.
- **Dependency Wait**: Ensures all dependsOn tasks are completed.
- **Execution**: Calls the appropriate plugin method (ReadAsync, WriteAsync, etc.).
- **Error Handling**: Applies retry logic, fallbacks, or fails the workflow.
- **Post-processing**: Logs result, stores state/output, propagates values to downstream tasks.

### Execution Modes
- **Standalone Binary**: Lightweight self-hosted engine
- **Docker Container**: For cloud-native or CI/CD deployments
- **API Triggered**: REST API for remote or event-based workflow starts
- **CLI Tool (flowctl)**: Used to run, validate, and manage workflows
- **Human-in-the-Loop**: Supports tasks that pause execution until manual approval

### Runtime Context
- Supports shared variables, secure secrets, plugin buffers, metadata

## Why FlowSynx?
Building scalable, maintainable, and cross-platform workflow automation is a complex challenge—especially 
when dealing with diverse environments, evolving requirements, and a growing set of integrations. 
FlowSynx is purpose-built to solve these challenges with a modular, extensible approach rooted in simplicity, 
flexibility, and developer empowerment.

## Features
Here are just a few of the many features that make FlowSynx powerful:

- Plugin-Based Extensibility
- Cross-Platform Execution (Windows, Linux, macOS, Docker, Cloud)
- JSON-Based Workflow Definition
- Command-Line Interface (CLI)
- .NET SDK for Programmatic Control
- Built-in Authentication and Security (JWT, Basic)
- Trigger-Based Execution (schedules, webhooks, file changes, etc.)
- Human-in-the-Loop (HITL) Steps
- Logging, Monitoring, and Auditing Hooks
- Flexible Error Handling and Retry Policies per task and workflow level
- Plugin Registry (Marketplace)
- REST-API Accessibility

## Architecture overview
<img src="/img/architecture-diagram.jpg">

### Intraction tools
- **CLI Interface**: Command-line tools for interacting with the FlowSynx system, enabling workflow management and execution from terminals.
- **REST API Gateway**: Provides secure, HTTP/HTTPS RESTful APIs to integrate with external systems, allowing remote workflow control and status querying.
- **SDK (Library)**: Developer-friendly libraries exposing FlowSynx functionalities programmatically, enabling custom applications to embed or automate workflow operations.

### FlowSynx Core
- **Workflow Orchestrator**: The core engine that loads and executes workflows defined as JSON DAGs.
- **Plugin Manager**: Dynamically loads plugins and maintains a plugin marketplace/registry for easy discovery and management.
- **Security & Auth**: Handles authentication and authorization for both REST API and CLI access, ensuring secure operations.
- **Logging & Auditing**: Tracks workflow execution, plugin activity, and audit trails for compliance and debugging.
- **Trigger Engine**: Listens for external events or schedules workflows to start based on timers, webhooks, or system signals.
- **Error handling**: Built-in support for task retries, timeouts, and fallbacks ensures reliable execution even in unstable environments. Custom retry strategies can be defined per task.

### Execution environments
- **Deployment & Execution Environments**: Supports flexible deployment models from standalone desktop/server installs to cloud containerized orchestration, with cross-platform compatibility.

### User Interfaces

#### Flowctl (CLI Interface)
Flowctl provides a simple yet powerful command-line interface for managing and running workflows directly from your terminal.

| Windows | Linux |
|---------|-------|
| ![Flowctl Windows CLI](/img/flowctl_windows.jpg) | ![Flowctl Linux CLI](/img/flowctl_linux.jpg) |

#### Web UI Console
The FlowSynx Web Console gives users a visual dashboard to manage workflows, monitor executions, view logs, and interact with running tasks in real time.

![FlowSynx Web Console Screenshot](/img/console.png)


## Get Started using FlowSynx

See our [Getting Started](https://flowsynx.io/docs/getting-started) guide over in our docs.

## Related Repositories

| Repo | Description |
|:-----|:------------|
| [FlowSynx](https://github.com/flowsynx/flowsynx) | The main repository that you are currently in. Contains the FlowSynx runtime code and overview documentation.
| [FlowCtl](https://github.com/flowsynx/flowctl) | The FlowCtl allows you to setup FlowSynx on your local dev machine, launches and manages FlowSynx instance.
| [Docs](https://flowsynx.io/docs/overview) | The documentation for FlowSynx.
| [Plugin Core](https://github.com/flowsynx/plugin-core) | Plugin interface for create new plugin for FlowSynx engine.
| [FlowPack ](https://github.com/flowsynx/flowpack) | A lightweight CLI tool designed to build, publish, and package FlowSynx-compatible plugins into a deployable .fspack file.
| [C# SDK](https://github.com/flowsynx/csharp-sdk) | C# SDK for integrating and executing
| [Plugin Registry](https://github.com/flowsynx/plugin-registry) | The hub for discovering, publishing, and managing plugins that enhance your FlowSynx automation workflows.
| [Plugin Template Project](https://github.com/flowsynx/plugin-template-project) | Ready-to-use Class Library template for .NET, designed to help you quickly set up a clean and consistent starting point for your plugin.
| [Plugins](#) | Plugins (Azure, JSON, CSV, etc)

## Community & Contributing
We welcome contributors of all experience levels! You can:
- Submit issues and feature requests
- Build and publish your own plugin
- Participate in our community discussions
- Help expand the system and documentation

👉 See [CONTRIBUTING.md](https://github.com/flowsynx/flowsynx/blob/master/CONTRIBUTING.md)

## License

This is free software under the terms of the MIT license (check the [LICENSE](https://github.com/flowsynx/flowsynx/blob/master/LICENSE) file included in this package).