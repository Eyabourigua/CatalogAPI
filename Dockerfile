# 1. Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
USER app

# 2. Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the .csproj
COPY ["src/ProductCatalog.Api/ProductCatalog.Api.csproj", "ProductCatalog.Api/"]

# Restore dependencies
RUN dotnet restore "ProductCatalog.Api/ProductCatalog.Api.csproj"

# Copy the source code
COPY src/ProductCatalog.Api/ ProductCatalog.Api/

WORKDIR "/src/ProductCatalog.Api"

# Build
RUN dotnet build "ProductCatalog.Api.csproj" -c Release -o /app/build

# 3. Publish
FROM build AS publish
RUN dotnet publish "ProductCatalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ProductCatalog.Api.dll"]
