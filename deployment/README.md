## 部署方式

#### 启动前准备

安装 `kubernetes` 。可通过 `curl -sfL https://get.k3s.io | sh -` 快速安装。

准备数据目录。在目录中放入 `appsettings.json`（可参考 `data/appsettings.json`） 和 `sqlite.db`。

修改 `pv.yml` 中 `/spec/local/path` 指定的挂载路径为准备好的数据目录。修改最后一行为部署主机的名称。主机名称可通过 `kubectl get node` 查看。

应用 `pv.yml` ，以启用一个 `local` 持久卷。

应用 `storageClass.yml` ，以启用 `local-storage` 类。

**可选**

设置 `tls` 证书，用于为 `ingress` 配置 `https`。
域名的证书可通过 `acme.sh` 自动获取并刷新。
获取证书后可通过 `kubectl create secret tls tls -n njupshw --cert ../cert/hw.pem --key ../cert/hw.key` 导入。

修改 `ingress.yml` 中域名信息并应用，以使用 `ingress` 对外提供服务。

#### 启动

依次应用 `depolyment.yml` 及 `service.yml` 即可。

#### Github Action 自动部署

确保命名空间 `njupshw` 存在，可使用 `kubectl create namespace njupshw` 创建。

在 `Github` 仓库页面，进入 `Settings/Secrets/Actions` 页面，创建三个 `secret` ：

* `DOCKER_HUB_USERNAME` 和 `DOCKER_HUB_ACCESS_TOKEN` 分别为 `Docker Hub` 的用户名与 Token 。可前往 [Docker Hub](https://hub.docker.com/settings/security?generateToken=true) 创建。
* `KUBECONFIG` 部署使用的 `kubeconfig` 。可使用 `kubectl config view --raw` 查看。注意 `server` 一栏的地址需要更换成服务器的外网IP。

此后每次推送至 `Github` 即可自动构建并部署。

#### 重启应用

使用 `kubectl replace --force -n njupshw -f deployment.yml` 即可重启应用。

