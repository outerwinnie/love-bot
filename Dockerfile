FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project files
COPY . ./

# Restore dependencies
RUN dotnet restore

# Build the application
RUN dotnet publish -c Release -o /out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published app from the build stage
COPY --from=build /out ./

# Set environment variables
ENV BOT_TOKEN=""
ENV CHANNEL_ID=""
ENV MESSAGE_DELAY_DAYS="3"

# Run the bot
CMD ["dotnet", "DiscordBot.dll"]
