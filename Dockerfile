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
WORKDIR /app
COPY --from=build /app ./
# The two folders that hold all changing data (database, logs, backups, keys / uploaded images).
# Created here and owned by the non-root "app" user so mounted volumes are writable.
RUN mkdir -p /app/App_Data /app/wwwroot/uploads && chown -R app:app /app/App_Data /app/wwwroot/uploads
USER app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Site__TrustAllProxies=true
EXPOSE 8080
VOLUME ["/app/App_Data", "/app/wwwroot/uploads"]
ENTRYPOINT ["dotnet", "BeautyByNegin.Web.dll"]
