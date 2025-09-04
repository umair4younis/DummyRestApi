# Dummy REST API Example

This project is a simple implementation of a RESTful API that interacts with employee data. It provides endpoints for CRUD operations and is built using the .NET Framework 4.8.

## Project Structure

- **Controllers**: Contains the API controllers that handle HTTP requests.
- **Services**: Contains business logic and interacts with repositories.
- **Repositories**: Contains data access logic for interacting with the database.
- **Models**: Contains the data models used in the application.
- **Authentication**: Contains authentication logic for user validation and token generation.
- **Logging**: Contains logging functionality for tracking and debugging.
- **Data**: Contains the database context and configuration settings.

## Setup Instructions

1. **Clone the repository**:
   ```
   git clone <repository-url>
   cd DummyRestApiExample
   ```

2. **Open the solution** in your preferred IDE.

3. **Restore NuGet packages**:
   ```
   dotnet restore
   ```

4. **Configure the database connection** in `DatabaseConfig.cs` to point to your MS SQL database.

5. **Run the application**:
   ```
   dotnet run
   ```

## API Endpoints

### Employee Endpoints

- **GET /api/employees**: Retrieve a list of all employees.
- **GET /api/employees/{id}**: Retrieve a specific employee by ID.
- **POST /api/employees**: Create a new employee.
- **PUT /api/employees/{id}**: Update an existing employee.
- **DELETE /api/employees/{id}**: Delete an employee.

## Testing

Unit tests are included in the `DummyRestApiExample.Tests` project. To run the tests, use the following command:

```
dotnet test
```

## Logging

The application includes logging functionality to track requests and errors. Logs can be found in the configured logging output.

## Contributing

Contributions are welcome! Please submit a pull request or open an issue for any enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the LICENSE file for details.