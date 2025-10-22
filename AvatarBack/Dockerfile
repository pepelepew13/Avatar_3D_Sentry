# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Avatar_3D_Sentry.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish Avatar_3D_Sentry.csproj -c Release -o /app/publish

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Avatar_3D_Sentry.dll"]