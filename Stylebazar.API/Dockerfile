# Build context is the REPO ROOT (the app references sibling projects):
#   docker build -f Stylebazar.API/Dockerfile -t stylebazar-api .

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against just the .csproj graph first (cached until a project changes).
COPY Core.Persistence/Core.Persistence.csproj Core.Persistence/
COPY Core.Application/Core.Application.csproj Core.Application/
COPY Stylebazar.DAL/Stylebazar.DAL.csproj Stylebazar.DAL/
COPY Stylebazar.BLL/Stylebazar.BLL.csproj Stylebazar.BLL/
COPY Stylebazar.API/Stylebazar.API.csproj Stylebazar.API/
RUN dotnet restore Stylebazar.API/Stylebazar.API.csproj

COPY . .
RUN dotnet publish Stylebazar.API/Stylebazar.API.csproj -c Release -o /app --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app ./

# Uploads dir owned by the non-root user. The API is stateless JWT (no key ring).
RUN mkdir -p /app/wwwroot/uploads && chown -R $APP_UID /app/wwwroot/uploads
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Stylebazar.API.dll"]
