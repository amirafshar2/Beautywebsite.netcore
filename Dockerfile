# Beauty by Negin — container image
#   docker compose up -d --build     (see README, "Docker ile çalıştırma")
# Debian-based images include ICU (Persian/Jalali calendar, Turkish/German cultures) and tzdata (time zones).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BeautyByNegin.sln ./
COPY src/BeautyByNegin.DataAccess/BeautyByNegin.DataAccess.csproj src/BeautyByNegin.DataAccess/
COPY src/BeautyByNegin.Business/BeautyByNegin.Business.csproj src/BeautyByNegin.Business/
COPY src/BeautyByNegin.Web/BeautyByNegin.Web.csproj src/BeautyByNegin.Web/
RUN dotnet restore src/BeautyByNegin.Web/BeautyByNegin.Web.csproj
COPY src/ src/
COPY docs/ docs/
RUN dotnet publish src/BeautyByNegin.Web/BeautyByNegin.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# ffmpeg compresses the treatment videos uploaded in the panel
RUN apt-get update \
 && apt-get install -y --no-install-recommends ffmpeg \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
# ALL changing data (database, keys, logs, backups, uploaded images/videos) lives in /app/App_Data:
# one Docker volume (docker-compose) or one Render disk (render.yaml) keeps everything.
RUN mkdir -p /app/App_Data && chown -R app:app /app/App_Data
ENV ASPNETCORE_ENVIRONMENT=Production \
    Site__TrustAllProxies=true
EXPOSE 8080
VOLUME ["/app/App_Data"]
# Starts as root only to prepare the data folder (a freshly mounted disk/volume may belong to root),
# then runs the app as the unprivileged "app" user. Port: $PORT (Render sets it) or 8080.
ENTRYPOINT ["/bin/sh", "-c", "mkdir -p /app/App_Data && chown -R app:app /app/App_Data && export ASPNETCORE_HTTP_PORTS=${PORT:-8080} && exec setpriv --reuid=app --regid=app --init-groups dotnet BeautyByNegin.Web.dll \"$@\"", "--"]
