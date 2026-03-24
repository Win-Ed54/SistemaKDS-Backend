# 1. Imagen de ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# 2. SDK para compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto para restaurar dependencias (Caché eficiente)
COPY ["kdspro.Api/kdspro.Api.csproj", "kdspro.Api/"]
COPY ["kdspro.Application/kdspro.Application.csproj", "kdspro.Application/"]
COPY ["kdspro.Domain/kdspro.Domain.csproj", "kdspro.Domain/"]
COPY ["kdspro.Infrastructure/kdspro.Infrastructure.csproj", "kdspro.Infrastructure/"]

RUN dotnet restore "kdspro.Api/kdspro.Api.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar el proyecto
WORKDIR "/src/kdspro.Api"
RUN dotnet build "kdspro.Api.csproj" -c Release -o /app/build

# 3. Etapa de publicación
FROM build AS publish
RUN dotnet publish "kdspro.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Imagen final (Runtime)
FROM base AS final
WORKDIR /app
# Copiamos todo lo publicado (esto ya incluye la carpeta wwwroot automáticamente si está en tu proyecto)
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "kdspro.Api.dll"]
