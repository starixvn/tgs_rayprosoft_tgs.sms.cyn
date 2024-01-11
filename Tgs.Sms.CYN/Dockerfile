#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Sms.CYN/Tgs.Sms.CYN.csproj", "Tgs.Sms.CYN/"]
COPY ["CYN.Core/CYN.Core.csproj", "CYN.Core/"]
RUN dotnet restore "./Tgs.Sms.CYN/./Tgs.Sms.CYN.csproj"
COPY . .
WORKDIR "/src/Tgs.Sms.CYN"
RUN dotnet build "./Tgs.Sms.CYN.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration=Release
RUN dotnet publish "./Tgs.Sms.CYN.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Sms.CYN.dll"]