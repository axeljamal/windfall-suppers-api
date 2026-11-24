# Build stage: restore and publish with the full SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/WindfallSuppers.Api/WindfallSuppers.Api.csproj src/WindfallSuppers.Api/
RUN dotnet restore src/WindfallSuppers.Api/WindfallSuppers.Api.csproj
COPY src/ src/
RUN dotnet publish src/WindfallSuppers.Api -c Release -o /app --no-restore

# Run stage: the small ASP.NET runtime image, as a non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Suppers="Data Source=/data/suppers.db"
USER app
VOLUME /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "WindfallSuppers.Api.dll"]
