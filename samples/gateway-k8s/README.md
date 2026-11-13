# gateway-k8s：K8s watch 动态路由验证

在 kind 集群里跑通 `Girvs.Gateway`（K8s 发现源）的最小验证系统：一个网关 + 两个 dummy 后端服务（`echo-a`、`echo-b`），验证 watch 能感知 Service 增删、YARP 路由随之秒级热更新。

## 组成

- `Gateway/`：最小 ASP.NET Core 网关项目，`ProjectReference` 直接引 `Girvs.Gateway`（独立于 `GirvsAspireSample.slnx`，不用 Aspire 服务发现）。
- `Dockerfile`：runtime-only 镜像（`mcr.microsoft.com/dotnet/aspnet:10.0` + 宿主 `dotnet publish` 产物），避免在 docker 内构建全仓库。
- `k8s.yaml`：ServiceAccount + ClusterRole（`services` 的 `list`/`watch`，注意是集群范围，非命名空间级 `Role`）+ ClusterRoleBinding；`gateway` Deployment/Service；`echo-a`/`echo-b`（`hashicorp/http-echo`）Deployment/Service。

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
sleep 3
curl -s -w "\n%{http_code}\n" http://localhost:18080/echo-a/   # hello from echo-a / 200
curl -s -w "\n%{http_code}\n" http://localhost:18080/echo-b/   # hello from echo-b / 200

# 5. 验证动态更新（核心）：删除 Service → watch 事件驱动路由消失（秒级，无需等 30s 轮询）
kubectl delete svc echo-b
sleep 5
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:18080/echo-b/   # 404

kubectl apply -f samples/gateway-k8s/k8s.yaml   # 重新加回
kubectl scale deploy/echo-b --replicas=1
sleep 5
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:18080/echo-b/   # 恢复 200
```

## 常见问题

- **RBAC 必须用 `ClusterRole`，不是命名空间级 `Role`**：`KubernetesGatewayServiceSource` 用 `ListServiceForAllNamespacesAsync` 做集群范围 list/watch，绑定命名空间级 `Role` 会在网关日志里看到 `services is forbidden: ... at the cluster scope`（2 秒退避后持续重试，不会崩溃，但拿不到任何服务）。`k8s.yaml` 已用 `ClusterRole`+`ClusterRoleBinding`。
- **`kind load docker-image` 对某些多平台镜像报 `content digest ... not found`**：这是本地 docker 内容库对该镜像的 attestation/多平台 manifest 不完整所致，非网关代码问题。绕过方式：
  ```bash
  docker save hashicorp/http-echo:1.0 | docker exec -i <kind-node-容器名> tee /tmp/http-echo.tar > /dev/null
  docker exec <kind-node-容器名> ctr --namespace=k8s.io images import --digests --snapshotter=overlayfs /tmp/http-echo.tar
  ```
- **网关本身也会出现在路由表里**：`KubernetesGatewayServiceSource` relist 的是集群内全部 Service（含 `kubernetes`、`kube-dns`、`gateway` 自身等），约定路由对它们同样生效，属预期行为，不影响验证。
