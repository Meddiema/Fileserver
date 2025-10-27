# Use the official .NET SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything
COPY . .

# Restore and build the project
RUN dotnet restore "FileServer.csproj"
RUN dotnet publish "FileServer.csproj" -c Release -o /app/out

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FileServer.dll"]
