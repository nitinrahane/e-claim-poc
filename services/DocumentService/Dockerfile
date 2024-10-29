# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the CSPROJ file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the entire application and build it
COPY . ./
RUN dotnet publish -c Release -o out

# Use the .NET ASP.NET Runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expose the port the app runs on
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "DocumentService.dll"]
