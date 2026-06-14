FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore as a separate layer for better caching
COPY src/FieldTaskManager.Domain/FieldTaskManager.Domain.csproj src/FieldTaskManager.Domain/
COPY src/FieldTaskManager.Application/FieldTaskManager.Application.csproj src/FieldTaskManager.Application/
COPY src/FieldTaskManager.Infrastructure/FieldTaskManager.Infrastructure.csproj src/FieldTaskManager.Infrastructure/
COPY src/FieldTaskManager.Api/FieldTaskManager.Api.csproj src/FieldTaskManager.Api/
RUN dotnet restore src/FieldTaskManager.Api/FieldTaskManager.Api.csproj

COPY src/ src/
RUN dotnet publish src/FieldTaskManager.Api/FieldTaskManager.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FieldTaskManager.Api.dll"]
