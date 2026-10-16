# Two stages: the first one (with the full .NET SDK) builds the app, the second one (only the small ASP.NET runtime)
# runs it. The SDK and the source code never reach the final image.

# ---- Stage 1: build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Only the project files first. NuGet packages are downloaded in this layer, and Docker keeps it as long as the .csproj
# files are unchanged, so a code-only change does not download everything again. (The .slnx is not needed: the web project
# references the other projects, and that is all "restore" and "publish" look at.)
COPY src/SocialNetworkPlatformProject/SocialNetworkPlatformProject.csproj src/SocialNetworkPlatformProject/
COPY src/SocialNetworkPlatformProject.Infrastructure/SocialNetworkPlatformProject.Infrastructure.csproj src/SocialNetworkPlatformProject.Infrastructure/
COPY src/SocialNetworkPlatformProject.Persistence/SocialNetworkPlatformProject.Persistence.csproj src/SocialNetworkPlatformProject.Persistence/
COPY src/Core/SocialNetworkPlatformProject.Application/SocialNetworkPlatformProject.Application.csproj src/Core/SocialNetworkPlatformProject.Application/
COPY src/Core/SocialNetworkPlatformProject.Domain/SocialNetworkPlatformProject.Domain.csproj src/Core/SocialNetworkPlatformProject.Domain/
RUN dotnet restore src/SocialNetworkPlatformProject/SocialNetworkPlatformProject.csproj

# Now the rest of the code (.dockerignore keeps bin, obj, uploads, logs and secrets out), then the real build.
COPY src/ src/
RUN dotnet publish src/SocialNetworkPlatformProject/SocialNetworkPlatformProject.csproj \
    -c Release -o /app/publish --no-restore

# ---- Stage 2: run ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# The app looks for its folders (wwwroot, App_Data, logs) next to the dll, so the published files go straight into /app.
WORKDIR /app
COPY --from=build /app/publish .

# The three folders the app writes to. They are created here, owned by the app user, because a named volume mounted on an
# empty folder copies that folder's owner: without this the app could not save an upload, a key or a log line.
#   wwwroot/uploads : pictures and videos that people upload
#   App_Data        : the login-token signing key and the data-protection keys (password-reset links)
#   logs            : the daily log files
RUN mkdir -p /app/wwwroot/uploads /app/App_Data /app/logs \
    && chown -R $APP_UID:$APP_UID /app/wwwroot/uploads /app/App_Data /app/logs

# The Microsoft images define APP_UID (the built-in non-root "app" user). Never run the web server as root.
USER $APP_UID

# Port 8080, because a non-root user may not listen on 80. The Microsoft aspnet image already sets
# ASPNETCORE_HTTP_PORTS=8080, so nothing needs to be set here (setting ASPNETCORE_URLS as well only made the app print a
# "overriding HTTP_PORTS" warning). Plain http: HTTPS has to come from a reverse proxy in front.
EXPOSE 8080

ENTRYPOINT ["dotnet", "SocialNetworkPlatformProject.dll"]
