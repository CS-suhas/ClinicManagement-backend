# Stage 1, building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ARG TARGET_RID=linux-x64

COPY src/ClinicManagement.Api/ClinicManagement.Api.csproj src/ClinicManagement.Api/
COPY src/ClinicManagement.Application/ClinicManagement.Application.csproj src/ClinicManagement.Application/
COPY src/ClinicManagement.Domain/ClinicManagement.Domain.csproj src/ClinicManagement.Domain/
COPY src/ClinicManagement.Infrastructure/ClinicManagement.Infrastructure.csproj src/ClinicManagement.Infrastructure/
RUN dotnet restore src/ClinicManagement.Api/ClinicManagement.Api.csproj --runtime $TARGET_RID

COPY src/ src/
RUN dotnet publish src/ClinicManagement.Api/ClinicManagement.Api.csproj \
    --configuration Release \
    --runtime $TARGET_RID \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    --output /app/publish \
    --no-restore

# Stage 2, runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["./ClinicManagement.Api"]
