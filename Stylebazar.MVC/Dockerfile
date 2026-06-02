# Build context is the REPO ROOT (the app references sibling projects):
#   docker build -f Stylebazar.MVC/Dockerfile -t stylebazar-mvc .

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against just the .csproj graph first — this layer is cached until a
# project file changes, so code-only edits skip the slow restore.
COPY Core.Persistence/Core.Persistence.csproj Core.Persistence/
COPY Core.Application/Core.Application.csproj Core.Application/
COPY Stylebazar.DAL/Stylebazar.DAL.csproj Stylebazar.DAL/
COPY Stylebazar.BLL/Stylebazar.BLL.csproj Stylebazar.BLL/
COPY Stylebazar.MVC/Stylebazar.MVC.csproj Stylebazar.MVC/
RUN dotnet restore Stylebazar.MVC/Stylebazar.MVC.csproj

# Copy the full source and publish.
COPY . .
RUN dotnet publish Stylebazar.MVC/Stylebazar.MVC.csproj -c Release -o /app --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app ./

# Writable dirs (uploads + data-protection key ring) owned by the image's
# built-in non-root user. Persisting the key ring keeps cookies/antiforgery
# valid across restarts; mount a volume here for durability.
RUN mkdir -p /app/wwwroot/uploads /app/keys && chown -R $APP_UID /app/wwwroot/uploads /app/keys
ENV ASPNETCORE_URLS=http://+:8080 \
    DataProtection__KeyRingPath=/app/keys
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Stylebazar.MVC.dll"]
