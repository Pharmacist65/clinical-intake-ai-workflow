FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/ClinicalIntake.Api/ClinicalIntake.Api.csproj backend/ClinicalIntake.Api/
RUN dotnet restore backend/ClinicalIntake.Api/ClinicalIntake.Api.csproj

COPY backend/ClinicalIntake.Api/ backend/ClinicalIntake.Api/
RUN dotnet publish backend/ClinicalIntake.Api/ClinicalIntake.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ClinicalIntake.Api.dll"]
