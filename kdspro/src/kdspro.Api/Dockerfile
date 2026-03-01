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
COPY src/kdspro.Api/kdspro.Api.csproj src/kdspro.Api/
COPY src/kdspro.Application/kdspro.Application.csproj src/kdspro.Application/
COPY src/kdspro.Domain/kdspro.Domain.csproj src/kdspro.Domain/
COPY src/kdspro.Infrastructure/kdspro.Infrastructure.csproj src/kdspro.Infrastructure/

# Restore
RUN dotnet restore

# Copiar todo
COPY . .

# Build
WORKDIR /src/src/kdspro.Api
RUN dotnet build kdspro.Api.csproj -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish kdspro.Api.csproj -c Release -o /app/publish

# Runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "kdspro.Api.dll"]
