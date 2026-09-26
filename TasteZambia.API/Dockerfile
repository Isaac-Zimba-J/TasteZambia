# Multi-stage: the SDK builds, the runtime image ships. The published output is
# ~100 MB rather than the ~800 MB the SDK would be.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore first, with only the project files, so a source change does not
# invalidate the restore layer. The API references Shared, so both are needed
# here - restoring with only the API's csproj fails on the missing reference.
COPY ["TasteZambia.API/TasteZambia.API.csproj", "TasteZambia.API/"]
COPY ["TasteZambia.Shared/TasteZambia.Shared.csproj", "TasteZambia.Shared/"]
RUN dotnet restore "TasteZambia.API/TasteZambia.API.csproj"

COPY TasteZambia.API/ TasteZambia.API/
COPY TasteZambia.Shared/ TasteZambia.Shared/
RUN dotnet publish "TasteZambia.API/TasteZambia.API.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Npgsql probes for GSSAPI and logs an error on every start without it. The
# connection does not need Kerberos, but a log that says "Error" on boot
# teaches everyone to ignore boot errors.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
 && rm -rf /var/lib/apt/lists/*

# Media blobs live here; compose mounts a named volume over it so they survive
# a rebuild. Owned by the non-root user the runtime image runs as.
RUN mkdir -p /app/media && chown $APP_UID /app/media
USER $APP_UID

EXPOSE 8080
COPY --from=build /app/publish .

# Kestrel serves HTTP inside the container; TLS terminates at nginx-proxy.
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "TasteZambia.API.dll"]
