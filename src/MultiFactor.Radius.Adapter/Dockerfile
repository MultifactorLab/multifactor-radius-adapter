#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN apt-get update && apt-get install -y libldap-2.4-2
RUN ln -s libldap-2.4.so.2 /usr/lib/x86_64-linux-gnu/libldap.so.2
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MultiFactor.Radius.Adapter/MultiFactor.Radius.Adapter.csproj", "MultiFactor.Radius.Adapter/"]
RUN dotnet restore "MultiFactor.Radius.Adapter/MultiFactor.Radius.Adapter.csproj"
COPY . .
WORKDIR "/src/MultiFactor.Radius.Adapter"
RUN dotnet build "MultiFactor.Radius.Adapter.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MultiFactor.Radius.Adapter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "multifactor-radius-adapter.dll"]
