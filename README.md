# Hardware Management API

A REST API built with ASP.NET Core and SQLite for managing brand data. This project was created as a portfolio project to practice building and working with HTTP APIs using C# and .NET.

## Features

- Retrieve brand/ram data
- Create, update, and delete brands/ram
- Filter and sort results
- Limit the number of returned records
- Update resources using JSON Patch
- Validate incoming requests and handle errors
- Persist data using SQLite
  
## Technologies

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Dapper
- [TruSql](https://github.com/Indra-Putra2/TruSql)

## Getting Started

### Prerequisites

- .NET SDK compatible with the project
- Git

### Run the application

Clone the repository:

```bash
git clone https://github.com/Indra-Putra2/HardwareInventory.Api
cd <repository-directory>
```

Restore dependencies and run the application:

```bash
dotnet restore
dotnet run
```

Check the application configuration for the database path and connection string.

## Example Request

Retrieve ram data:
```http
GET /api/Ram
```

Example response:
```json
[
  {
    "id": 1,
    "brandId": 1,
    "name": "Crucial Pro Overclocking 32 GB",
    "brandName": "Crucial",
    "memoryType": "DDR5",
    "memorySpeed": 6000,
    "module": "2 x 16GB",
    "totalCapacity": 32,
    "firstWordLatency": 12,
    "casLatency": 36,
    "rating": 4.5,
    "reviewCount": 45,
    "color": "['Black']",
    "price": 273.99,
    "pricePerGB": 8.56,
    "date": null,
    "time": null,
    "dateTime": null,
    "sold": null
  }
]
```

Retrieve brand data:

```http
GET /api/Brand
```

Example response:

```json
[
  {
    "id": 1,
    "name": "Corsair"
  }
]
```


The response above is illustrative. Actual fields and results depend on the API implementation and database contents.

## Purpose

This project demonstrates my ability to build a REST API using C# and ASP.NET Core, work with HTTP methods and status codes, validate requests, and interact with a relational database.

## Future Improvements

- Add automated tests
- Improve API documentation
- Expand validation and error handling where needed
