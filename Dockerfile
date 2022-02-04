﻿FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BuyIt.Api/BuyIt.Api.csproj", "BuyIt.Api/"]
RUN dotnet restore "BuyIt.Api/BuyIt.Api.csproj"
COPY . .
WORKDIR "/src/BuyIt.Api"
RUN dotnet build "BuyIt.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BuyIt.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BuyIt.Api.dll"]
