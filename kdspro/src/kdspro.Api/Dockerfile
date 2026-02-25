# 1. IMAGEN DE COMPILACIÓN (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar
COPY ["kdspro.Api/kdspro.Api.csproj", "kdspro.Api/"]
COPY ["kdspro.Domain/kdspro.Domain.csproj", "kdspro.Domain/"]
COPY ["kdspro.Infrastructure/kdspro.Infrastructure.csproj", "kdspro.Infrastructure/"]

RUN dotnet restore "kdspro.Api/kdspro.Api.csproj"

# Copiar todo y compilar
COPY . .
WORKDIR "/src/kdspro.Api"
RUN dotnet build "kdspro.Api.csproj" -c Release -o /app/build

# 2. PUBLICACIÓN
FROM build AS publish
RUN dotnet publish "kdspro.Api.csproj" -c Release -o /app/publish

# 3. IMAGEN FINAL (RUNTIME)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "kdspro.Api.dll"]