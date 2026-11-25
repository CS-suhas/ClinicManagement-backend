# Stage 1, building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:8c6edfb26c85153f236d26efebd9ad349aec403583f343f201ee67838b23658d AS build
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
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.15@sha256:e6702e45635272155c2aa35568bcbdb975595c0afa73e2656a973d0a87703282 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["./ClinicManagement.Api"]
