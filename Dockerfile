# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SocialMediaBot.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Create volume for logs
VOLUME /app/logs

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SocialMediaBot.dll"]
