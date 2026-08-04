# Dockerfile da API .NET 8 (seção 9 — build multi-stage).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
# JWT — override em produção via secrets/variável do orquestrador.
ENV Jwt:Key=change-me-in-production-at-least-32-characters-long-please
ENV Jwt:Issuer=admin-api
ENV Jwt:Audience=admin-mobile
EXPOSE 8080
ENTRYPOINT ["dotnet", "AdminApi.dll"]
