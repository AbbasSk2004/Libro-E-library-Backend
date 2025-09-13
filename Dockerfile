# Use the official .NET 9.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["E-Library.API.csproj", "."]
RUN dotnet restore "E-Library.API.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "E-Library.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "E-Library.API.csproj" -c Release -o /app/publish

# Create the final runtime image
FROM base AS final
WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Create directory for uploads
RUN mkdir -p /app/wwwroot/uploads/id-cards

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Expose port 80
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "E-Library.API.dll"]
