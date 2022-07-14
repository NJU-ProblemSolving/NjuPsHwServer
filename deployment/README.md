## 部署方式

#### 启动前准备

安装 `kubernetes` 。可通过 `curl -sfL https://get.k3s.io | sh -` 快速安装。

修改 `pv.yml` 挂载路径和最后一行部署主机的名称。在挂载路径中放入 `appsettings.json`（参考 `data/appsettings.json`） 和 `sqlite.db`。主机名称可通过 `kubectl get node` 查看。

应用 `pv.yml` ，以启用一个 `local` 持久卷。

应用 `storageClass.yml` ，以启用 `local-storage` 类。

#### 启动

依次应用 `depolyment.yml` 及 `service.yml` 即可。

#### Github Action 自动部署

确保命名空间 `njupshw` 存在，可使用 `kubectl create namespace njupshw` 创建。

在 `Github` 仓库页面，进入 `Settings/Secrets/Actions` 页面，创建三个 `secret` ：

* `DOCKER_HUB_USERNAME` 和 `DOCKER_HUB_ACCESS_TOKEN` 分别为 `Docker Hub` 的用户名与 Token 。可前往 [Docker Hub](https://hub.docker.com/settings/security?generateToken=true) 创建。
* `KUBECONFIG` 部署使用的 `kubeconfig` 。可使用 `kubectl config view --raw` 查看。注意 `server` 一栏的地址需要更换成服务器的外网IP。

此后每次推送至 `Github` 即可自动构建并部署。

