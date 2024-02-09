FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
ARG BUILD_CONFIGURATION=Release  
WORKDIR /server

COPY . .
RUN dotnet restore AzurePractiseTaskV2.sln

WORKDIR /server/AzurePractiseTaskV2
RUN dotnet build -c $BUILD_CONFIGURATION -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS server_runtime
WORKDIR /app

EXPOSE 8080
EXPOSE 8081

COPY --from=builder /server/AzurePractiseTaskV2/out .
ENTRYPOINT ["dotnet", "AzurePractiseTaskV2.dll", "--environment=Development"]