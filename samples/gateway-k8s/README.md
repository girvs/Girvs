# gateway-k8s：K8s 服务目录动态路由验证

在 kind 集群里跑通 `Girvs.Gateway` + `Girvs.ServiceGovernance`（Kubernetes 服务目录）的最小验证系统：一个网关 + 两个 dummy 后端服务（`echo-a`、`echo-b`），验证服务目录能感知 Service 增删，YARP 路由随之热更新。

## 组成

- `Gateway/`：最小 ASP.NET Core 网关项目，`ProjectReference` 直接引 `Girvs.Gateway`（独立于 `GirvsAspireSample.slnx`，不使用 Girvs 启动器）。通过 `AddGirvsServiceDirectory` 注册 Kubernetes 服务目录，验证场景把 `DiscoveryRefreshInterval` 缩短为 5 秒。
- `Gateway/appsettings.FlowProtection.json`：流程防护示例配置，默认 `Enabled: false`。
- `Dockerfile`：runtime-only 镜像（`mcr.microsoft.com/dotnet/aspnet:10.0` + 宿主 `dotnet publish` 产物），避免在 docker 内构建全仓库。
- `k8s.yaml`：
  - ServiceAccount + ClusterRole（`services` 的 `list`，集群范围）+ ClusterRoleBinding；
  - `gateway` Deployment / Service；
  - `echo-a` / `echo-b`（`hashicorp/http-echo`）Deployment / Service，Service 带有标签 `girvs.io/business: "true"`、注解 `girvs.io/gateway-enabled: "true"` 与 `girvs.io/gateway-endpoint: "http"`，端口命名为 `http`。

## 服务如何进入网关路由

1. `KubernetesServiceDirectoryProvider` 按标签选择器 `girvs.io/business=true` 列出 Service（未设置 `KubernetesNamespace` 时为全部命名空间）；
2. 每个 Service 端口生成一个端点：地址 `http://{name}.{namespace}.svc.cluster.local:{port}`，端点名为端口名（未命名时为 `port-{port}`）；
3. 注解 `girvs.io/gateway-enabled: "true"` 且 `girvs.io/gateway-endpoint` 指向存在的端点名时，网关生成路由 `/{name}/{**catch-all}`，转发时去掉 `/{name}` 前缀。

缺少标签的 Service（`kubernetes`、`kube-dns`、`gateway` 自身等）不会进入服务目录；有标签但缺少注解的 Service 会进入服务目录，但不生成网关路由。

## 复现步骤

```bash
# 0. 准备一个 kind 集群（已有可跳过）
kind create cluster --name girvs-gw

# 1. 宿主 publish（避免在容器内构建全仓库/拉 SDK 镜像）
dotnet publish samples/gateway-k8s/Gateway/Gateway.csproj -c Release -o samples/gateway-k8s/publish

# 2. 构建 runtime-only 镜像并加载进 kind
docker build -t girvs-gw-sample:test -f samples/gateway-k8s/Dockerfile samples/gateway-k8s
kind load docker-image girvs-gw-sample:test --name girvs-gw

# echo 后端镜像：若 kind load 因多平台 manifest 报 "content digest ... not found"，
# 改用 docker save + 容器内 ctr images import（见下方“常见问题”）
docker pull hashicorp/http-echo:1.0
kind load docker-image hashicorp/http-echo:1.0 --name girvs-gw

# 3. 部署
kubectl apply -f samples/gateway-k8s/k8s.yaml
kubectl wait --for=condition=available deploy/gateway --timeout=120s

# 4. 验证初始路由
kubectl port-forward svc/gateway 18080:80 &
sleep 8
curl -s -w "\n%{http_code}\n" http://localhost:18080/echo-a/   # hello from echo-a / 200
curl -s -w "\n%{http_code}\n" http://localhost:18080/echo-b/   # hello from echo-b / 200

# 5. 验证动态更新：删除 Service → 下一次服务目录刷新后路由消失
kubectl delete svc echo-b
sleep 8
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:18080/echo-b/   # 404

kubectl apply -f samples/gateway-k8s/k8s.yaml   # 重新加回
sleep 8
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:18080/echo-b/   # 恢复 200

# 6. 验证注解控制：取消公开 → 路由消失
kubectl annotate svc echo-a girvs.io/gateway-enabled=false --overwrite
sleep 8
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:18080/echo-a/   # 404
```

路由变化的延迟上限约为一个 `DiscoveryRefreshInterval`（样例为 5 秒，框架默认 30 秒）。

## 常见问题

- **路由表为空 / 全部 404**：检查业务 Service 是否带有标签 `girvs.io/business: "true"`、注解 `girvs.io/gateway-enabled: "true"`，以及 `girvs.io/gateway-endpoint` 是否与 Service 端口名一致（未命名端口需写 `port-{port}`）。
- **RBAC 必须用 `ClusterRole`，不是命名空间级 `Role`**：未设置 `KubernetesNamespace` 时，服务目录用 `ListServiceForAllNamespacesAsync` 做集群范围 list，绑定命名空间级 `Role` 会在网关日志里看到 `服务目录刷新失败，保留最后成功快照` 及 `services is forbidden: ... at the cluster scope`。如需最小权限，可在 `ServiceGovernanceConfig` 中设置 `KubernetesNamespace`，并改用 `Role` + `RoleBinding`。
- **`kind load docker-image` 对某些多平台镜像报 `content digest ... not found`**：这是本地 docker 内容库对该镜像的 attestation/多平台 manifest 不完整所致，非网关代码问题。绕过方式：
  ```bash
  docker save hashicorp/http-echo:1.0 | docker exec -i <kind-node-容器名> tee /tmp/http-echo.tar > /dev/null
  docker exec <kind-node-容器名> ctr --namespace=k8s.io images import --digests --snapshotter=overlayfs /tmp/http-echo.tar
  ```
- **启用流程防护**：把 `Gateway/appsettings.FlowProtection.json` 中的 `Enabled` 改为 `true`，并提供可访问的 Redis 资源与 `Girvs.Cache` 的 `IRedisConnectionWrapper` 注册，详见 [Girvs.Gateway README](../../Girvs.Gateway/README.md#流程防护)。
