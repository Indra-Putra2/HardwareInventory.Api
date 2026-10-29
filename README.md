# Hardware Management API

A REST API built with ASP.NET Core and SQLite for managing hardware inventory data, including RAM specifications and brand information.

This project serves as a portfolio project to demonstrate my ability to build APIs using C# and .NET, handle HTTP requests, validate input, and interact with a database.

## Features

- Retrieve brand and RAM data
- Create, update, and delete brands and RAM records
- Filter and sort results
- Limit the number of returned records
- Update resources using JSON Patch
- Validate incoming requests and handle errors
- Persist data using SQLite

## Technologies

- **Language:** C#
- **Framework:** ASP.NET Core Web API
- **ORM:** Entity Framework Core
- **Database:** SQLite
- **Data access:** Dapper
- **Custom library:** [TruSql](https://github.com/Indra-Putra2/TruSql)

## TruSql

This project also uses [TruSql](https://github.com/Indra-Putra2/TruSql), a library I developed.

Including TruSql in this project demonstrates its use alongside an ASP.NET Core API and a database-backed application.

## Getting Started

### Prerequisites

- .NET SDK compatible with the project
- Git

### Installation

Clone the repository:

```bash
git clone https://github.com/Indra-Putra2/HardwareInventory.Api.git
cd HardwareInventory.Api
```

Restore dependencies and run the application:

```bash
dotnet restore
dotnet run
```

Check the application configuration for the database connection string and path.

### API Documentation

If Swagger/OpenAPI is enabled, run the application and open the Swagger UI URL configured by the project to explore and test the available endpoints.

## Example Requests

### Retrieve RAM data

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
    "color": ["Black"],
    "price": 273.99,
    "pricePerGB": 8.56,
    "date": null,
    "time": null,
    "dateTime": null,
    "sold": null
  }
]
```

### Retrieve brand data

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

The example responses illustrate the expected response structure. Actual results depend on the database contents and API implementation.

## Purpose

This project demonstrates my ability to develop a REST API using C# and ASP.NET Core, work with HTTP methods and status codes, validate requests, and integrate database access technologies.

It also provides an opportunity to apply my own reusable library, TruSql, in a practical application.

## Future Improvements

- Add automated tests
- Improve API documentation
- Expand validation and error handling where needed
