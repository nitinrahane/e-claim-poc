# Use the .NET SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore the .csproj
COPY DocumentProcessor.csproj ./
RUN dotnet restore

# Copy all source code and build the project
COPY . ./
RUN dotnet publish -c Release -o /out

# Use the ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .

# Set the entry point to start the application
ENTRYPOINT ["dotnet", "DocumentProcessor.dll"]
