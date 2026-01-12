FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project files
COPY ["IconProject/IconProject.csproj", "IconProject/"]
COPY ["IconProject.Database/IconProject.Database.csproj", "IconProject.Database/"]
COPY ["IconProject.Common/IconProject.Common.csproj", "IconProject.Common/"]

# Restore dependencies
RUN dotnet restore "IconProject/IconProject.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/IconProject"
RUN dotnet build "./IconProject.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./IconProject.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IconProject.dll"]
