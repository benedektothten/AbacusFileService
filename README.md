# AbacusFileService

AbacusFileService is a small ASP\.NET Core Web API for managing files in Azure Blob Storage.  
It exposes endpoints to list, upload, download, and delete files, with centralized error handling and Docker support.

## Tech Stack

- .NET 9\.0 (ASP\.NET Core Web API)
- Azure Blob Storage
- C\#
- Docker

## Features

- List all files in a container
- Upload files (optional custom blob name)
- Generate SAS URLs for secure downloads
- Delete files
- Centralized error handling with correlation IDs
- OpenAPI endpoint for interactive API docs (in Development)

## Configuration

Configure Azure settings in `appsettings.json` or via environment variables:

```json
{
  "AzureSettings": {
    "BlobConnectionString": "your-connection-string",
    "ContainerName": "your-container-name",
    "BlobTokenExpiryInMinutes": 60
  }
}
```

## Docker support

Run the application in docker with these commands:
```
  docker build -t abacusfileservice .
  docker run -p 8080:8080 -p 8081:8081 abacusfileservice
```
