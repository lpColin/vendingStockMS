FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["VendingStock.Api.csproj", "./"]
RUN dotnet restore "VendingStock.Api.csproj"
COPY . .
RUN dotnet publish "VendingStock.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "VendingStock.Api.dll"]
