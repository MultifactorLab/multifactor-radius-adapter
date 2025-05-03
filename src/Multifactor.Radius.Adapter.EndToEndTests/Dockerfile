FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y libldap-2.5-0
RUN ln -s libldap-2.5.so.0 /usr/lib/x86_64-linux-gnu/libldap.so.2
WORKDIR /app
EXPOSE 80
EXPOSE 443