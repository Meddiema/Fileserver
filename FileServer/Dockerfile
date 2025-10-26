# Step 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.sln .
COPY FileServer/*.csproj ./FileServer/
RUN dotnet restore

COPY FileServer/. ./FileServer/
WORKDIR /app/FileServer
RUN dotnet publish -c Release -o /out

# Step 2: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://*:5000
ENTRYPOINT ["dotnet", "FileServer.dll"]
