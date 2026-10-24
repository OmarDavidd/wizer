# syntax=docker/dockerfile:1
# Build context: the repository root (dotnet-tools.json lives there).
#   docker build -f apps/backend/Dockerfile --target api .
#   docker build -f apps/backend/Dockerfile --target migrator .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached while only the code changes.
COPY dotnet-tools.json ./
COPY apps/backend/Directory.Build.props apps/backend/Wizer.slnx apps/backend/
COPY apps/backend/src/Wizer.Api/Wizer.Api.csproj apps/backend/src/Wizer.Api/packages.lock.json apps/backend/src/Wizer.Api/
RUN dotnet tool restore \
    && dotnet restore apps/backend/src/Wizer.Api --locked-mode

COPY apps/backend/ apps/backend/
RUN dotnet publish apps/backend/src/Wizer.Api -c Release --no-restore -o /out/api \
    && dotnet ef migrations bundle --project apps/backend/src/Wizer.Api --configuration Release -o /out/migrator/efbundle

# The API. The .NET images listen on 8080 and ship the non-root "app" user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /out/api .
USER $APP_UID
ENTRYPOINT ["dotnet", "Wizer.Api.dll"]

# One-shot: applies pending migrations, then exits. Pass the target with `--connection "<connection string>"`.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=build /out/migrator .
USER $APP_UID
ENTRYPOINT ["./efbundle"]
