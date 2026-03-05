# Imagen base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solution
COPY kdspro.sln .

# Copiar proyectos
COPY kdspro.Api/kdspro.Api.csproj kdspro.Api/
COPY kdspro.Application/kdspro.Application.csproj kdspro.Application/
COPY kdspro.Domain/kdspro.Domain.csproj kdspro.Domain/
COPY kdspro.Infrastructure/kdspro.Infrastructure.csproj kdspro.Infrastructure/

# Restore
RUN dotnet restore

# Copiar todo el código
COPY . .

# Build
WORKDIR /src/kdspro.Api
RUN dotnet build kdspro.Api.csproj -c Release -o /app/build

# Publish
FROM build AS publish
WORKDIR /src/kdspro.Api
RUN dotnet publish kdspro.Api.csproj -c Release -o /app/publish

# Runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "kdspro.Api.dll"]
