#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AE.PID.Server/AE.PID.Server.csproj", "AE.PID.Server/"]
RUN dotnet restore "AE.PID.Server/AE.PID.Server.csproj"
COPY . .
WORKDIR "/src/AE.PID.Server"
RUN dotnet build "AE.PID.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AE.PID.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

# 设置工作目录
WORKDIR /opt/aepid
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AE.PID.Server.dll"]