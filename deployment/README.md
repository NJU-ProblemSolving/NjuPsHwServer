# 项目部署

本项目使用容器化部署，分为前端和后端两个部分，可通过 `docker-compose` 或 `kubernetes` 一键部署。

## 配置

前端部分无需配置。

后端部分的配置均位于 `/data/` 目录下，包含：

* 后端配置文件 `appsettings.json`
* 数据库 `sqlite.db`

具体配置方式如下：

* `appsettings.json` 需要管理员根据实际情况手动配置。可参考 JSON Schema `appsettings.schema.json` ，或基于 `appsettings.example.json` 修改。
* `sqlite.db` 为数据库文件，通常由 Entity Framework Core 自动创建。可参考 [EF Core 官方文档](https://learn.microsoft.com/en-us/ef/core/get-started/overview/install#get-the-net-core-cli-tools) 安装相关辅助工具，并使用 `dotnet ef database update` 生成空白数据库。

以上为必要配置，还有一些可选功能需要额外配置：

* `appsettings.json` 中的 `Smtp` 部分为邮件发送配置。在重置密码等功能中使用。
* `appsettings.json` 中的 `Jwt` 部分为 `JWT` 认证方式的配置。在 Web API 的 Authentication 和 Authorization 中使用，通常可以在辅助脚本中用 JWT 快速登录并调用 API。
* `appsettings.json` 中的 `OpenIdConnect` 部分为 `OIDC` 认证方式的配置。在 Web API 的 Authentication 中使用，用于提供“从OJ登录”功能，需要和 OJ 的 Identity Server 共同配置。

## 部署

在开发环境准备好配置文件后，可通过 `docker-compose` 或 `kubernetes` 部署。

### docker-compose

`docker-compose` 方式部署较为简单，可以一键启动完整的前后端环境。

部署需要安装 `docker` 和 `docker-compose` ，安装方法此处不再介绍。

复制 `docker-compose.yml` 到服务器上，并在目录中放入配置文件。此时目录结构如下：

```
./
├── docker-compose.yml
└── data/
    ├── appsettings.json
    ├── sqlite.db
    └── attachments/
```

修改 `docker-compose.yml` 中的 `services.reverse-proxy.ports` 部分，将分号前的端口号改为需要在服务器上提供服务的端口号。例如，需要在服务器的 `8080` 端口上启动作业系统，则需要将其改为 `8080:80` 。

在该目录下执行 `docker-compose up -d` 即可启动服务。执行 `docker-compose down` 即可停止服务。执行 `docker-compose logs -f` 可以查看日志。

当镜像有更新时，执行 `docker-compose pull` 可以更新本地镜像。先 `down` 再 `up` 后即可更新运行的服务。

### kubernetes

`kubernetes` 方式部署较为复杂，但是在部署完成后可以实现 CI/CD ，即每次推送至 Github 仓库后自动构建并部署。但这要求 Github Actions 能够访问服务器上的 `kubernetes` 集群。

部署需要安装 `kubernetes` 并配置好集群。在只有一台服务器时可通过 `curl -sfL https://get.k3s.io | sh -` 快速安装 k3s。

复制 `deployment` 文件夹到服务器上，并在目录中放入配置文件。

* **准备持久存储卷**

    修改 `pv.yml` 中 `/spec/local/path` 指定的挂载路径为准备好的 `data` 目录。修改最后一行为部署主机的名称。主机名称可通过 `kubectl get node` 查看。

* **创建存储类**

    执行 `kubectl apply -f deployment/pv.yml` 以启用一个 `local` 持久卷。

    执行 `kubectl apply -f deployment/storageClass.yml` 以启用 `local-storage` 类。

* **启动前后端服务**
    
    依次应用 `deployment.yml` 及 `service.yml` 即可。具体命令如下：

    ```bash
    kubectl create namespace njupshw
    kubectl apply -n njupshw -f deployment/deployment.yml
    kubectl apply -n njupshw -f deployment/service.yml
    ```

    可以通过 `kubectl get all -n njupshw` 查看当前部署状态，使用 `kubectl logs -n njupshw statefulset/server` 查看服务器日志。

* **启动反向代理**

    修改 `ingress.yml` 中的 `host` 为服务器的外网IP或域名。
    
    启动 traefik 服务（若使用先前的命令安装 k3s 后会自动启动），并执行 `kubectl apply -n njupshw -f deployment/ingress.yml` 以启动反向代理。

    可以通过 `kubectl get all -n kube-system` 查看当前部署状态，使用 `kubectl logs -n kube-system deploy/traefik` 查看服务器日志。


以下为可选配置项：

* **设置 tls 证书**

    设置后可以为网站启用 `https`。

    域名的证书可通过 `acme.sh` 自动获取并刷新。

    获取证书后可通过 `kubectl create secret tls tls -n njupshw --cert ./cert/hw.pem --key ./cert/hw.key --dry-run=client --save-config -o yaml | kubectl apply -f -` 导入或更新证书。

    取消 `ingress.yml` 中 `spec.tls` 选项的注释，并修改其中的域名信息。重新应用 `ingress.yml` 。

    如果在申请证书时需要临时关闭 `ingress` 服务以开放 80 端口，可以使用 `kubectl -n kube-system scale deploy traefik --replicas 0` 将服务实例降为 0。完成后重新设为 1 即可。

* **Github Action 自动部署**

    在 Github 仓库页面，进入 `Settings/Secrets/Actions` 页面，创建三个 `secret` ：

    * `DOCKER_HUB_USERNAME` 和 `DOCKER_HUB_ACCESS_TOKEN` 分别为 `Docker Hub` 的用户名与 Token 。可前往 [Docker Hub](https://hub.docker.com/settings/security?generateToken=true) 创建。
    * `KUBE_CONFIG` 部署使用的 `kubeconfig` 。可使用 `kubectl config view --raw` 查看。注意 `server` 一栏的地址需要更换成 Github Actions 能够访问到的 k8s 集群的外网IP。
    * `KUBE_NAMESPACE` 为部署的命名空间。
    
    通过在 Github Environment 中配置不同的变量可以实现多环境部署。

    此后每次推送至 Github 即可自动构建并部署。

当服务出现意外情况时，可以使用 `kubectl replace --force -n njupshw -f deployment.yml` 强制重启应用。
