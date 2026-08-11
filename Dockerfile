# Multi-stage: build and publish with the SDK image, run on the smaller ASP.NET image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# The commit being built. CI passes it (docker build --build-arg GIT_SHA=$(git rev-parse HEAD));
# a bare local build leaves it unknown. SourceRevisionId bakes it into the assembly's informational
# version, which is what /version reads back at runtime. See Part 8 for where CI sets this.
ARG GIT_SHA=unknown
COPY . .
RUN dotnet publish src/Jig.Host/Jig.Host.csproj -c Release -o /app -p:SourceRevisionId=$GIT_SHA

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# $APP_UID is the non-root app user the base image provides.
USER $APP_UID
ENTRYPOINT ["dotnet", "Jig.Host.dll"]
