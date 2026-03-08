# 🏛️ PR Sentry

[![GitHub Action](https://img.shields.io/badge/GitHub%20Action-PR%20Sentry-purple?logo=github)](https://github.com/husseinbbassam/pr-sentry-action)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-blue?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> A containerized GitHub Action that analyzes your .NET solution's dependency graph
> and enforces **Clean Architecture** rules — automatically, on every Pull Request.

---

## ✨ Features

- 🔍 **Dependency Graph Analysis** — Parses `.sln`, `.slnx`, and `.csproj` files without requiring a build.
- 🧬 **Domain Isolation Rule** — Ensures your `Domain` project has zero internal dependencies.
- ⚙️ **Application Boundary Rule** — Ensures `Application` only references `Domain`.
- 🌐 **Web/API Boundary Rule** — Prevents your `Web`/`API` project from directly coupling to `Infrastructure` or `Data` layers.
- 💬 **PR Comments** — Posts (and updates) a rich Markdown report directly on the Pull Request.
- 🚨 **Strict Mode** — Optionally fail the CI build when violations are found.

---

## 🚀 Quick Start

Add the following step to your GitHub Actions workflow:

```yaml
# .github/workflows/architecture-check.yml
name: Architecture Check

on:
  pull_request:
    branches: [ main, develop ]

jobs:
  architecture:
    name: Clean Architecture Analysis
    runs-on: ubuntu-latest
    permissions:
      pull-requests: write   # required to post PR comments
      contents: read

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Run PR Sentry
        uses: husseinbbassam/pr-sentry-action@v1
        with:
          solution-path: MyApp.sln
          strict: 'true'
```

---

## ⚙️ Inputs

| Input            | Required | Default              | Description                                                                                   |
|------------------|----------|----------------------|-----------------------------------------------------------------------------------------------|
| `solution-path`  | ✅ Yes   | —                    | Path to the `.sln`, `.slnx`, or `.csproj` file relative to the repository root.              |
| `strict`         | ❌ No    | `false`              | When `true`, the action exits with code `1` if violations are found, failing the workflow.   |
| `github-token`   | ❌ No    | `${{ github.token }}`| GitHub token used to post the PR comment. The default `GITHUB_TOKEN` is sufficient.         |

---

## 📤 Outputs

| Output    | Description                                                                              |
|-----------|------------------------------------------------------------------------------------------|
| `summary` | Markdown-formatted summary of violations (or a success message). Use with `$GITHUB_OUTPUT`. |

---

## 🏗️ Architecture Rules

PR Sentry enforces three fundamental Clean Architecture dependency rules:

### Rule 1 – Domain Isolation 🧬

> **The Domain project must have zero dependencies on other internal projects.**

The Domain layer contains your pure business entities, value objects, and domain services.
It must remain completely free of any outward-facing concerns (repositories, HTTP clients,
databases, etc.). Allowing Domain to reference other layers would couple your business logic
to implementation details, making it impossible to test in isolation and difficult to evolve.

**Detected by:** Any project whose name contains `Domain`.

### Rule 2 – Application Layer Boundary ⚙️

> **The Application project may only depend on Domain.**

The Application layer orchestrates use cases. It defines interfaces (ports) for the services
it needs (e.g., `IUserRepository`, `IEmailSender`) but must not know about the concrete
implementations. Referencing `Infrastructure` or `Data` from `Application` breaks this
contract and tightly couples your use-case logic to delivery and storage mechanisms.

**Detected by:** Any project whose name contains `Application` or `App`.

### Rule 3 – Web/API Layer Boundary 🌐

> **The Web/API project must not directly reference Infrastructure or Data projects.**

The Presentation layer (Web API, MVC, gRPC host) should depend only on `Application`
(and by extension `Domain`). Infrastructure concerns (`DbContext`, repositories, email
senders, etc.) should be registered in the DI container at startup using interfaces
defined in `Application`. This enforces the Dependency Inversion Principle and ensures
your web layer is not accidentally bypassing the application's use-case layer.

**Detected by:** Any project whose name contains `Web`, `API`, `Api`, `Host`, `Presentation`, `Server`, `Mvc`, or `Grpc`.

---

## 📦 Layer Detection

PR Sentry detects layers by inspecting project name segments (split on `.`, `_`, `-`):

| Layer          | Detected by keywords                                                        |
|----------------|-----------------------------------------------------------------------------|
| 🧬 Domain      | `Domain`                                                                    |
| ⚙️ Application | `Application`, `App`                                                        |
| 🔧 Infrastructure | `Infrastructure`, `Infra`                                                |
| 🗄️ Data       | `Data`, `Persistence`, `Repository`, `Repositories`, `Dal`, `Db`, `Database` |
| 🌐 Web/API     | `Web`, `API`, `Api`, `Host`, `Presentation`, `Server`, `Mvc`, `Grpc`      |

Projects that do not match any keyword are reported as `Unknown` and are not subjected to the architecture rules.

---

## 💬 PR Comment Examples

### ✅ No Violations

```markdown
## 🏛️ PR Sentry – Clean Architecture Analysis

> **Analyzed:** `MyApp.sln`
> **Projects found:** 4
> **Run at:** 2025-06-01 12:00:00 UTC

✅ **No architectural violations found.** Your solution complies with Clean Architecture rules.

### 📦 Discovered Projects

| Project              | Layer             |
|----------------------|-------------------|
| `MyApp.Application`  | ⚙️ Application    |
| `MyApp.Domain`       | 🧬 Domain         |
| `MyApp.Infrastructure` | 🔧 Infrastructure |
| `MyApp.Web`          | 🌐 WebApi         |
```

### ❌ Violations Found

```markdown
## 🏛️ PR Sentry – Clean Architecture Analysis

❌ **2 architectural violation(s) detected.**

### ❌ Rule: Domain Isolation

| Severity | Project      | Description                                                                 |
|----------|--------------|-----------------------------------------------------------------------------|
| ❌ Error | `MyApp.Domain` | Domain project 'MyApp.Domain' has a forbidden dependency on 'MyApp.Application'. |

### ❌ Rule: Web/API Layer Boundary

| Severity | Project    | Description                                                                          |
|----------|------------|--------------------------------------------------------------------------------------|
| ❌ Error | `MyApp.Web` | Web/API project 'MyApp.Web' directly references 'MyApp.Infrastructure' (Infrastructure layer). |
```

---

## 🐳 Running Locally with Docker

```bash
docker build -t pr-sentry .

docker run --rm \
  -v /path/to/your/solution:/workspace \
  -e INPUT_SOLUTION_PATH=/workspace/MyApp.sln \
  -e INPUT_STRICT=true \
  pr-sentry
```

---

## 🔧 Development

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/) (optional, for container testing)

### Build & Test

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run unit tests
dotnet test

# Publish self-contained binary
dotnet publish src/PrSentryAction/PrSentryAction.csproj \
  --configuration Release \
  --output ./publish
```

### Project Structure

```
pr-sentry-action/
├── src/
│   └── PrSentryAction/
│       ├── Configuration/     # ActionInputs – reads env vars
│       ├── Models/            # ProjectInfo, ArchitecturalViolation, AnalysisResult
│       ├── Parsers/           # SolutionParser – reads .sln/.slnx/.csproj
│       ├── Rules/             # DomainRule, ApplicationRule, WebApiRule
│       ├── Services/          # ArchitectureAnalyzer, MarkdownFormatter, GitHubService
│       └── Program.cs         # Entry point
├── tests/
│   └── PrSentryAction.Tests/
│       ├── Parsers/           # SolutionParser tests
│       ├── Rules/             # Rule engine tests
│       └── Services/          # Formatter tests
├── action.yml                 # GitHub Action metadata
├── Dockerfile                 # Container definition
└── README.md
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

_Powered by PR Sentry 🛡️ — keeping your .NET architecture clean, one PR at a time._
