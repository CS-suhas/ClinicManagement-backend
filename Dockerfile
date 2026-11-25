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
FROM mcr.microsoft.com/dotnet/runtime-deps:11.0.0@sha256:b392cb6e185b0c02cd8066b8b3b2e78d58f7cc15ba5d52ea3e2bfe8f3c0060f9 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["./ClinicManagement.Api"]
