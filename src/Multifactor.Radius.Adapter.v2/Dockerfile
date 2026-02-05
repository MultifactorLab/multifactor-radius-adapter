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

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

USER root

# Устанавливаем библиотеки LDAP
RUN apt-get update && \
    apt-get install -y \
    libldap-2.4-2 \
    libldap-common \
    libsasl2-2 \
    libsasl2-modules && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Создаем симлинк в директории .NET
RUN DOTNET_DIR=$(dirname $(find /usr/share/dotnet -name "System.Private.CoreLib.dll" | head -1)) && \
    ln -sf /usr/lib/x86_64-linux-gnu/libldap-2.4.so.2 $DOTNET_DIR/libldap-2.4.so.2 && \
    ln -sf /usr/lib/x86_64-linux-gnu/liblber-2.4.so.2 $DOTNET_DIR/liblber-2.4.so.2

USER app

# Копируем опубликованное приложение
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RadiusV2.dll"]