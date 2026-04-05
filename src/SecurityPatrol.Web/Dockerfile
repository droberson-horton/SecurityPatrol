FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/SecurityPatrol.Web/SecurityPatrol.Web.csproj", "src/SecurityPatrol.Web/"]
RUN dotnet restore "src/SecurityPatrol.Web/SecurityPatrol.Web.csproj"
COPY . .
WORKDIR "/src/src/SecurityPatrol.Web"
RUN dotnet build "SecurityPatrol.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SecurityPatrol.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p wwwroot/uploads
ENTRYPOINT ["dotnet", "SecurityPatrol.Web.dll"]
