# Multi-stage: build and publish with the SDK image, run on the smaller ASP.NET image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Jig.Host/Jig.Host.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# $APP_UID is the non-root app user the base image provides.
USER $APP_UID
ENTRYPOINT ["dotnet", "Jig.Host.dll"]
