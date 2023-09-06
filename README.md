# NjuPsHw

问题求解作业系统 （NjuPsHw）能够统计学生的作业完成及订正情况，提供作业提交、作业批改、作业订正、成绩查询等功能。

本仓库为后端部分。项目部署文档（包括前端及后端）请见[这里](deployment/README.md)。

## 后端开发文档

使用到的技术如下：

* 使用 `C#` 语言开发
* `Dotnet 5+`
* 使用 `ASP Net Core` 提供 Web API 服务
* 使用 `Entity Framework Core` 作为 ORM 框架
* 使用 Github Actions 配置 CI/CD

#### Github Actions

Github Actions 配置文件位于 `.github/workflows` 目录下。

* build

    在每次 push 时，自动构建当前分支的代码，并将构建结果上传到 Docker Hub 中。构建过程中生成的 artifacts 可在 Actions 页面中下载。

    如需要令 Github Actions 自动推送构建的镜像至 Docker Hub ，则需要配置好 Github Secrets 中的 `DOCKER_HUB_USERNAME` 和 `DOCKER_HUB_ACCESS_TOKEN`。

* deploy

    在每次 push 到 `main`、`develop` 分支时，将构建结果自动部署到服务器上。

    详细配置请见 [部署文档](deployment/README.md)。

* codegen

    在每次使用 `git tag` 发布新的 API 版本时，自动生成对应的客户端代码，并上传至 Github Release 方便其他项目使用。版本号通常为 `v1.0.0` 这样的格式。
