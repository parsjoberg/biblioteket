FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["biblioteket.Web/biblioteket.Web.csproj", "biblioteket.Web/"]
COPY ["biblioteket.ServiceDefaults/biblioteket.ServiceDefaults.csproj", "biblioteket.ServiceDefaults/"]
RUN dotnet restore "biblioteket.Web/biblioteket.Web.csproj"
COPY . .
WORKDIR "/src/biblioteket.Web"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "biblioteket.Web.dll"]