FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore WebHoanTien.sln
RUN dotnet publish src/WebHoanTien.Web/WebHoanTien.Web.csproj -c Release -o /app/web --no-restore
RUN dotnet publish src/WebHoanTien.DbMigrator/WebHoanTien.DbMigrator.csproj -c Release -o /app/migrator --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/web .
ENTRYPOINT ["dotnet", "WebHoanTien.Web.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS migrator
WORKDIR /app
COPY --from=build /app/migrator .
ENTRYPOINT ["dotnet", "WebHoanTien.DbMigrator.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS render
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
COPY --from=build /app/web ./web
COPY --from=build /app/migrator ./migrator
COPY render-entrypoint.sh ./render-entrypoint.sh
RUN chmod +x ./render-entrypoint.sh
ENTRYPOINT ["./render-entrypoint.sh"]
