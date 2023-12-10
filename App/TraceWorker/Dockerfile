# Use the official .NET Core SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

# Set the working directory inside the container
WORKDIR /app

# Copy the entire project and build
COPY . ./
RUN dotnet publish TraceWorker/TraceWorker.csproj -c Release -o out

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .

# Define the entry point
ENTRYPOINT ["dotnet", "TraceWorker.dll"]