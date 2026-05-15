FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Web_prototype/Web_prototype.csproj", "Web_prototype/"]
COPY ["DataModels/DataModels.csproj", "DataModels/"]
RUN dotnet restore "Web_prototype/Web_prototype.csproj"

COPY . .
WORKDIR "/src/Web_prototype"
RUN dotnet build "Web_prototype.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Web_prototype.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN chown -R $APP_UID /app
USER $APP_UID
ENTRYPOINT ["dotnet", "Web_prototype.dll"]