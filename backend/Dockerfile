# Multi-stage Dockerfile for ASP.NET Core 8 Web API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY LeaveManagement.Core/LeaveManagement.Core.csproj LeaveManagement.Core/
COPY LeaveManagement.Infrastructure/LeaveManagement.Infrastructure.csproj LeaveManagement.Infrastructure/
COPY LeaveManagement.Api/LeaveManagement.Api.csproj LeaveManagement.Api/

RUN dotnet restore LeaveManagement.Api/LeaveManagement.Api.csproj

# Copy all source code and build
COPY . .
WORKDIR /src/LeaveManagement.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "LeaveManagement.Api.dll"]
