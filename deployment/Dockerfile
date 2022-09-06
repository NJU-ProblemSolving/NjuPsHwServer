# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
# COPY *.sln .
COPY *.csproj ./
RUN dotnet restore -r linux-x64 

# copy everything else and build app
COPY . ./
RUN dotnet publish -c release -r linux-x64 --no-self-contained -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app ./
COPY Assets ./
ENTRYPOINT ["dotnet", "NjuCsCmsHelper.Server.dll"]