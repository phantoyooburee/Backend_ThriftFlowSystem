# Use the ASP.NET Core runtime image as the base for the final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the csproj file and restore dependencies
COPY ["Backend_ThriftFlowSystem/Backend_ThriftFlowSystem.csproj", "Backend_ThriftFlowSystem/"]
RUN dotnet restore "./Backend_ThriftFlowSystem/Backend_ThriftFlowSystem.csproj"

# Copy the rest of the source code and build
COPY . .
WORKDIR "/src/Backend_ThriftFlowSystem"
RUN dotnet build "./Backend_ThriftFlowSystem.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Backend_ThriftFlowSystem.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage: create the runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Backend_ThriftFlowSystem.dll"]
