FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RadiusV2/RadiusV2.csproj", "RadiusV2/"]
RUN dotnet restore "RadiusV2/RadiusV2.csproj"
COPY . .
WORKDIR "/src/RadiusV2"
RUN dotnet build "RadiusV2.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RadiusV2.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Начинаем финальную стадию
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Устанавливаем OpenLDAP и создаем симлинк
RUN apt-get update && \
    apt-get install -y libldap-2.5-0 && \
    ln -s /usr/lib/x86_64-linux-gnu/libldap-2.5.so.0 /usr/lib/x86_64-linux-gnu/libldap-2.4.so.2 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Копируем опубликованное приложение из стадии publish
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RadiusV2.dll"]