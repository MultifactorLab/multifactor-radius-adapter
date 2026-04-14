FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Устанавливаем библиотеки LDAP
RUN apt-get update && \
    apt-get install -y \
    libldap-2.5-0 \
    libldap-common \
    libsasl2-2 \
    libsasl2-modules && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Создаем симлинк в директории .NET
RUN mkdir -p /usr/share/dotnet/shared/Microsoft.NETCore.App/8.0.25/

RUN ln -sf /usr/lib/x86_64-linux-gnu/libldap-2.5.so.0 /usr/share/dotnet/shared/Microsoft.NETCore.App/8.0.25/libldap-2.4.so.2 && \
    ln -sf /usr/lib/x86_64-linux-gnu/liblber-2.5.so.0 /usr/share/dotnet/shared/Microsoft.NETCore.App/8.0.25/liblber-2.5.so.0 

RUN mkdir -p /etc/ldap && \
    echo "TLS_REQCERT never" > /etc/ldap/ldap.conf
USER $APP_UID
WORKDIR /app

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Multifactor.Radius.Adapter.v2/Multifactor.Radius.Adapter.v2.csproj", "Multifactor.Radius.Adapter.v2/"]
COPY ["../../Multifactor.Core.Ldap/src/Multifactor.Core.Ldap/Multifactor.Core.Ldap/Multifactor.Core.Ldap.csproj", "../../Multifactor.Core.Ldap/src/Multifactor.Core.Ldap/Multifactor.Core.Ldap/"]
COPY ["Multifactor.Radius.Adapter.v2.Application/Multifactor.Radius.Adapter.v2.Application.csproj", "Multifactor.Radius.Adapter.v2.Application/"]
COPY ["Multifactor.Radius.Adapter.v2.Infrastructure/Multifactor.Radius.Adapter.v2.Infrastructure.csproj", "Multifactor.Radius.Adapter.v2.Infrastructure/"]
RUN dotnet restore "Multifactor.Radius.Adapter.v2/Multifactor.Radius.Adapter.v2.csproj"
COPY . .
WORKDIR "/src/Multifactor.Radius.Adapter.v2"
RUN dotnet build "./Multifactor.Radius.Adapter.v2.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM  --platform=linux/amd64 build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Multifactor.Radius.Adapter.v2.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM --platform=linux/amd64 base AS final
        
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Multifactor.Radius.Adapter.v2.dll"]

