# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

ARG REVISION=unknown

# copy csproj and restore as distinct layers
# COPY *.sln .
COPY *.csproj ./
RUN dotnet restore -r linux-x64

# copy everything else and build app
COPY . ./
RUN sed -i "s/Revision = \"unknown\";/Revision = \"$REVISION\";/" Config.cs
RUN cat Config.cs
RUN dotnet publish -c release -r linux-x64 --no-self-contained -o /app --no-restore

RUN dotnet new tool-manifest && \
    dotnet tool install --version 6.4.0 Swashbuckle.AspNetCore.Cli && \
    dotnet swagger tofile --output /app/swagger.json /app/NjuCsCmsHelper.Server.dll v1 && \
    sed -i 's/"additionalProperties": { }/"additionalProperties": false/' /app/swagger.json

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app /app
COPY Assets /app/Assets
ENTRYPOINT ["dotnet", "NjuCsCmsHelper.Server.dll"]
