# Rename Aspire Gateway To Girvs Gateway Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `Girvs.Aspire.Gateway` 模块完整改名为通用的 `Girvs.Gateway`，同时保持现有网关 API 和运行行为不变。

**Architecture:** 这是一次边界改名迁移，不改服务发现、路由生成、流程防护或配置结构。先迁移项目路径和命名空间，再更新引用方、测试和文档，最后用网关测试、Hosting 契约测试、解决方案构建和 `git diff --check` 验证。

**Tech Stack:** .NET SDK 10.0.100、C#、YARP、KubernetesClient、Consul、xUnit、Aspire sample projects。

## Global Constraints

- 新项目名为 `Girvs.Gateway`。
- 主命名空间为 `Girvs.Gateway`、`Girvs.Gateway.Configuration`、`Girvs.Gateway.Discovery`、`Girvs.Gateway.FlowProtection`。
- 测试项目名为 `Girvs.Gateway.Tests`，测试命名空间为 `Girvs.Gateway.Tests`。
- 不增加 `Girvs.Aspire.Gateway` 旧命名空间兼容层。
- 不重命名 `AddGirvsGateway`、`GatewayDiscoveryConfig`、`GatewayDiscoveryType`、`AspireGatewayServiceSource`、`ConsulGatewayServiceSource`、`KubernetesGatewayServiceSource`。
- 不修改 `GatewayDiscovery`、`GatewayDiscoveryConfig` 等运行时配置节名。
- 不删除 Consul 兼容能力。
- 不处理 `bin/`、`obj/`、`publish/` 生成产物，验证命令可重新生成它们。

---

## File Structure

- Rename directory: `Girvs.Aspire.Gateway/` -> `Girvs.Gateway/`
- Rename project: `Girvs.Gateway/Girvs.Aspire.Gateway.csproj` -> `Girvs.Gateway/Girvs.Gateway.csproj`
- Rename directory: `tests/Girvs.Aspire.Gateway.Tests/` -> `tests/Girvs.Gateway.Tests/`
- Rename project: `tests/Girvs.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj` -> `tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj`
- Modify: `Girvs.slnx` project paths
- Modify: `samples/Sample.Gateway/Sample.Gateway.csproj` project reference
- Modify: `samples/gateway-k8s/Gateway/Gateway.csproj` project reference
- Modify: `samples/Sample.Gateway/*.cs` using statements
- Modify: `samples/gateway-k8s/Gateway/Program.cs` using statements
- Modify: `tests/Girvs.Aspire.Hosting.Tests/SampleGatewayConsulContractTests.cs` renamed-path contract assertions
- Modify: all moved gateway source files under `Girvs.Gateway/` namespaces and using statements
- Modify: all moved gateway tests under `tests/Girvs.Gateway.Tests/` namespaces and using statements
- Modify docs: `CLAUDE.md`, `docs/aspire/apphost-guide.md`, `docs/aspire/升级方案.md`, `samples/gateway-k8s/README.md`, gateway README, relevant `docs/superpowers/specs/*.md`, relevant `docs/superpowers/plans/*.md`

### Task 1: Project And Test Project Rename

**Files:**
- Rename: `Girvs.Aspire.Gateway/` -> `Girvs.Gateway/`
- Rename: `Girvs.Gateway/Girvs.Aspire.Gateway.csproj` -> `Girvs.Gateway/Girvs.Gateway.csproj`
- Rename: `tests/Girvs.Aspire.Gateway.Tests/` -> `tests/Girvs.Gateway.Tests/`
- Rename: `tests/Girvs.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj` -> `tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj`
- Modify: `Girvs.Gateway/Girvs.Gateway.csproj`
- Modify: `tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj`
- Modify: `Girvs.slnx`

**Interfaces:**
- Consumes: current `Girvs.Aspire.Gateway` project and `Girvs.Aspire.Gateway.Tests` project.
- Produces: buildable `Girvs.Gateway` project and `Girvs.Gateway.Tests` project paths for later namespace and reference updates.

- [ ] **Step 1: Rename directories and project files**

Run:

```bash
mv Girvs.Aspire.Gateway Girvs.Gateway
mv Girvs.Gateway/Girvs.Aspire.Gateway.csproj Girvs.Gateway/Girvs.Gateway.csproj
mv tests/Girvs.Aspire.Gateway.Tests tests/Girvs.Gateway.Tests
mv tests/Girvs.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj
```

Expected: commands produce no output and all four destination paths exist.

- [ ] **Step 2: Update gateway project internals**

Edit `Girvs.Gateway/Girvs.Gateway.csproj` so the `InternalsVisibleTo` item is:

```xml
  <ItemGroup>
    <InternalsVisibleTo Include="Girvs.Gateway.Tests" />
  </ItemGroup>
```

Keep `TargetFramework`, `Description`, `ProjectReference`, `PackageReference` entries unchanged.

- [ ] **Step 3: Update test project reference**

Edit `tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj` so the project reference is:

```xml
  <ItemGroup>
    <ProjectReference Include="..\..\Girvs.Gateway\Girvs.Gateway.csproj" />
  </ItemGroup>
```

Keep package references unchanged.

- [ ] **Step 4: Update solution paths**

Edit `Girvs.slnx` so these entries exist:

```xml
    <Project Path="Girvs.Gateway/Girvs.Gateway.csproj" />
    <Project Path="tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj" />
```

Remove these entries:

```xml
    <Project Path="Girvs.Aspire.Gateway/Girvs.Aspire.Gateway.csproj" />
    <Project Path="tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj" />
```

- [ ] **Step 5: Verify solution references no old project path**

Run:

```bash
rg -n "Girvs\.Aspire\.Gateway/Girvs\.Aspire\.Gateway\.csproj|tests/Girvs\.Aspire\.Gateway\.Tests/Girvs\.Aspire\.Gateway\.Tests\.csproj" Girvs.slnx
```

Expected: no matches and exit code 1.

- [ ] **Step 6: Commit project rename shell**

Run:

```bash
git add Girvs.slnx Girvs.Gateway tests/Girvs.Gateway.Tests
git commit -m "refactor: 网关项目改名为 Girvs.Gateway"
```

Expected: commit succeeds.

### Task 2: Namespace Migration

**Files:**
- Modify: `Girvs.Gateway/**/*.cs`
- Modify: `tests/Girvs.Gateway.Tests/**/*.cs`

**Interfaces:**
- Consumes: renamed project paths from Task 1.
- Produces: source namespace `Girvs.Gateway` and test namespace `Girvs.Gateway.Tests` for samples and contracts to consume.

- [ ] **Step 1: Replace gateway namespaces and using statements**

Run:

```bash
perl -pi -e 's/Girvs\.Aspire\.Gateway/Girvs.Gateway/g' $(rg -l "Girvs\.Aspire\.Gateway" Girvs.Gateway tests/Girvs.Gateway.Tests)
```

Expected: command exits successfully and updates only files under `Girvs.Gateway` and `tests/Girvs.Gateway.Tests`.

- [ ] **Step 2: Verify no old namespace remains in moved code**

Run:

```bash
rg -n "Girvs\.Aspire\.Gateway" Girvs.Gateway tests/Girvs.Gateway.Tests
```

Expected: no matches and exit code 1.

- [ ] **Step 3: Build the renamed gateway test project enough to expose namespace errors**

Run:

```bash
dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --no-restore --nologo
```

Expected: no compile errors caused by `Girvs.Aspire.Gateway` namespaces. Test failures from external Redis/Testcontainers availability are acceptable only if the project compiles and the failure clearly happens at runtime.

- [ ] **Step 4: Commit namespace migration**

Run:

```bash
git add Girvs.Gateway tests/Girvs.Gateway.Tests
git commit -m "refactor: 更新网关命名空间"
```

Expected: commit succeeds.

### Task 3: Sample And Contract Reference Migration

**Files:**
- Modify: `samples/Sample.Gateway/Sample.Gateway.csproj`
- Modify: `samples/Sample.Gateway/Startup.cs`
- Modify: `samples/Sample.Gateway/SwaggerEndpointEnumerator.cs`
- Modify: `samples/gateway-k8s/Gateway/Gateway.csproj`
- Modify: `samples/gateway-k8s/Gateway/Program.cs`
- Modify: `tests/Girvs.Aspire.Hosting.Tests/SampleGatewayConsulContractTests.cs`

**Interfaces:**
- Consumes: `Girvs.Gateway/Girvs.Gateway.csproj` and `Girvs.Gateway.*` namespaces from Tasks 1-2.
- Produces: sample gateways that compile against `Girvs.Gateway`, plus contract tests that assert the new path.

- [ ] **Step 1: Update sample project references**

In `samples/Sample.Gateway/Sample.Gateway.csproj`, replace:

```xml
<ProjectReference Include="..\..\Girvs.Aspire.Gateway\Girvs.Aspire.Gateway.csproj" />
```

with:

```xml
<ProjectReference Include="..\..\Girvs.Gateway\Girvs.Gateway.csproj" />
```

In `samples/gateway-k8s/Gateway/Gateway.csproj`, replace:

```xml
<ProjectReference Include="..\..\..\Girvs.Aspire.Gateway\Girvs.Aspire.Gateway.csproj" />
```

with:

```xml
<ProjectReference Include="..\..\..\Girvs.Gateway\Girvs.Gateway.csproj" />
```

- [ ] **Step 2: Update sample using statements**

Run:

```bash
perl -pi -e 's/Girvs\.Aspire\.Gateway/Girvs.Gateway/g' samples/Sample.Gateway/Startup.cs samples/Sample.Gateway/SwaggerEndpointEnumerator.cs samples/gateway-k8s/Gateway/Program.cs
```

Expected: command exits successfully.

- [ ] **Step 3: Update Hosting contract test path checks**

In `tests/Girvs.Aspire.Hosting.Tests/SampleGatewayConsulContractTests.cs`, change the gateway project path read to:

```csharp
var gatewayProject = File.ReadAllText(Path.Combine(repoRoot, "Girvs.Gateway", "Girvs.Gateway.csproj"));
```

Change assertions that expect old project reference text to assert the new text:

```csharp
Assert.Contains("Girvs.Gateway.csproj", sampleGatewayProject);
Assert.Contains("Girvs.Gateway", gatewayProject);
```

Do not change assertions about `GatewayDiscovery__DiscoveryType`, `GatewayDiscoveryConfig`, `Consul`, `Aspire` or sample runtime behavior unless they reference the old project path/name.

- [ ] **Step 4: Verify sample and contract references no old namespace**

Run:

```bash
rg -n "Girvs\.Aspire\.Gateway|Girvs.Aspire.Gateway" samples/Sample.Gateway samples/gateway-k8s/Gateway tests/Girvs.Aspire.Hosting.Tests/SampleGatewayConsulContractTests.cs
```

Expected: no matches and exit code 1.

- [ ] **Step 5: Run Hosting contract tests**

Run:

```bash
dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --no-restore --nologo
```

Expected: tests pass.

- [ ] **Step 6: Commit sample migration**

Run:

```bash
git add samples/Sample.Gateway samples/gateway-k8s/Gateway tests/Girvs.Aspire.Hosting.Tests/SampleGatewayConsulContractTests.cs
git commit -m "refactor: 更新样例网关项目引用"
```

Expected: commit succeeds.

### Task 4: Documentation Migration

**Files:**
- Rename implicit through Task 1: `Girvs.Gateway/README.md`
- Modify: `Girvs.Gateway/README.md`
- Modify: `CLAUDE.md`
- Modify: `docs/aspire/apphost-guide.md`
- Modify: `docs/aspire/升级方案.md`
- Modify: `samples/gateway-k8s/README.md`
- Modify: relevant `docs/superpowers/specs/*.md`
- Modify: relevant `docs/superpowers/plans/*.md`

**Interfaces:**
- Consumes: final source project name `Girvs.Gateway`.
- Produces: docs that describe `Girvs.Gateway` as the generic gateway package while preserving historical context where needed.

- [ ] **Step 1: Replace module name in active docs**

Run:

```bash
perl -pi -e 's/Girvs\.Aspire\.Gateway/Girvs.Gateway/g' CLAUDE.md Girvs.Gateway/README.md docs/aspire/apphost-guide.md docs/aspire/升级方案.md samples/gateway-k8s/README.md
```

Expected: command exits successfully.

- [ ] **Step 2: Replace module name in relevant superpowers docs**

Run:

```bash
perl -pi -e 's/Girvs\.Aspire\.Gateway/Girvs.Gateway/g' $(rg -l "Girvs\.Aspire\.Gateway" docs/superpowers/specs docs/superpowers/plans)
```

Expected: command exits successfully.

- [ ] **Step 3: Review README title and purpose**

Ensure `Girvs.Gateway/README.md` starts with:

```markdown
# Girvs.Gateway
```

Ensure its introductory paragraph says the package is a generic Girvs YARP gateway and its discovery sources include Aspire、Consul、Kubernetes.

- [ ] **Step 4: Verify old project name remains only in migration design comparisons or generated artifacts**

Run:

```bash
rg -n "Girvs\.Aspire\.Gateway|Girvs.Aspire.Gateway" CLAUDE.md Girvs.Gateway docs samples tests Girvs.slnx
```

Expected: matches are allowed only in `docs/superpowers/specs/2026-07-22-rename-aspire-gateway-to-girvs-gateway-design.md` when showing before/after migration examples. If matches appear in source, tests, samples, active docs, or solution files, replace them with `Girvs.Gateway`.

- [ ] **Step 5: Commit documentation migration**

Run:

```bash
git add CLAUDE.md Girvs.Gateway/README.md docs samples/gateway-k8s/README.md
git commit -m "docs: 更新通用网关命名"
```

Expected: commit succeeds.

### Task 5: Final Verification And Cleanup

**Files:**
- Inspect: all changed files
- Do not edit: `bin/`, `obj/`, `samples/gateway-k8s/publish/`

**Interfaces:**
- Consumes: all migrated code and docs from Tasks 1-4.
- Produces: verified repository state for final handoff.

- [ ] **Step 1: Verify no old source/test/sample references remain**

Run:

```bash
rg -n "Girvs\.Aspire\.Gateway|Girvs.Aspire.Gateway" Girvs.Gateway tests samples Girvs.slnx CLAUDE.md docs/aspire
```

Expected: no matches and exit code 1.

- [ ] **Step 2: Run gateway tests**

Run:

```bash
dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --no-restore --nologo
```

Expected: tests pass. If Redis/Testcontainers tests fail because Docker is unavailable, capture the exact failing test names and rerun non-container tests with a filter that excludes `RedisFlowStateStoreTests`.

- [ ] **Step 3: Run Hosting tests**

Run:

```bash
dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --no-restore --nologo
```

Expected: tests pass.

- [ ] **Step 4: Build solution**

Run:

```bash
dotnet build Girvs.slnx --no-restore --nologo
```

Expected: build succeeds.

- [ ] **Step 5: Run diff whitespace check**

Run:

```bash
git diff --check
```

Expected: no output and exit code 0.

- [ ] **Step 6: Review final diff**

Run:

```bash
git diff --stat HEAD~4..HEAD
git diff -- Girvs.slnx Girvs.Gateway/Girvs.Gateway.csproj tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj samples/Sample.Gateway/Sample.Gateway.csproj samples/gateway-k8s/Gateway/Gateway.csproj
```

Expected: diff shows only the planned rename, reference updates, namespace updates and documentation updates.

- [ ] **Step 7: Commit final fixes if verification required edits**

If Steps 1-6 required additional edits, run:

```bash
git add Girvs.slnx Girvs.Gateway tests/Girvs.Gateway.Tests samples docs CLAUDE.md
git commit -m "fix: 修正网关改名遗漏引用"
```

Expected: commit succeeds only if there were additional edits. If no additional edits were needed, do not create an empty commit.
