# Use .NET SDK for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY FileServer/FileServer.csproj ./FileServer/
RUN dotnet restore ./FileServer/FileServer.csproj

# Copy everything and build
COPY . .
WORKDIR /src/FileServer
RUN dotnet publish FileServer.csproj -c Release -o /app/out

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FileServer.dll"]
