FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
 
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
 
COPY ["src/TeamFlow.API/TeamFlow.API.csproj", "src/TeamFlow.API/"]
COPY ["src/TeamFlow.Core/TeamFlow.Core.csproj", "src/TeamFlow.Core/"]
COPY ["src/TeamFlow.Infrastructure/TeamFlow.Infrastructure.csproj", "src/TeamFlow.Infrastructure/"]
 
RUN dotnet restore "src/TeamFlow.API/TeamFlow.API.csproj"
 
COPY . .
 
WORKDIR "/src/src/TeamFlow.API"
RUN dotnet build "TeamFlow.API.csproj" -c Release -o /app/build
 
FROM build AS publish
RUN dotnet publish "TeamFlow.API.csproj" -c Release -o /app/publish /p:UseAppHost=false
 
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TeamFlow.API.dll"]