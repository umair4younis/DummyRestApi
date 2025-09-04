using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using DummyRestApiExample.Models;
using DummyRestApiExample.Repositories;
using DummyRestApiExample.Services;

namespace DummyRestApiExample.Tests.Services
{
    [TestFixture]
    public class EmployeeServiceTests
    {
        private EmployeeService _employeeService;
        private Mock<IEmployeeRepository> _employeeRepositoryMock;

        [SetUp]
        public void SetUp()
        {
            _employeeRepositoryMock = new Mock<IEmployeeRepository>();
            _employeeService = new EmployeeService(_employeeRepositoryMock.Object);
        }

        [Test]
        public async Task GetAllEmployees_ReturnsListOfEmployees()
        {
            // Arrange
            var employees = new List<Employee>
            {
                new Employee { Id = 1, Name = "John Doe", Salary = 50000, Age = 30 },
                new Employee { Id = 2, Name = "Jane Smith", Salary = 60000, Age = 25 }
            };
            _employeeRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(employees);

            // Act
            var result = await _employeeService.GetAllEmployees();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("John Doe", result[0].Name);
        }

        [Test]
        public async Task GetEmployeeById_ExistingId_ReturnsEmployee()
        {
            // Arrange
            var employee = new Employee { Id = 1, Name = "John Doe", Salary = 50000, Age = 30 };
            _employeeRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(employee);

            // Act
            var result = await _employeeService.GetEmployeeById(1);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("John Doe", result.Name);
        }

        [Test]
        public async Task CreateEmployee_ValidEmployee_ReturnsCreatedEmployee()
        {
            // Arrange
            var employee = new Employee { Name = "John Doe", Salary = 50000, Age = 30 };
            _employeeRepositoryMock.Setup(repo => repo.CreateAsync(employee)).ReturnsAsync(employee);

            // Act
            var result = await _employeeService.CreateEmployee(employee);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("John Doe", result.Name);
        }

        [Test]
        public async Task UpdateEmployee_ExistingEmployee_ReturnsUpdatedEmployee()
        {
            // Arrange
            var employee = new Employee { Id = 1, Name = "John Doe", Salary = 50000, Age = 30 };
            _employeeRepositoryMock.Setup(repo => repo.UpdateAsync(employee)).ReturnsAsync(employee);

            // Act
            var result = await _employeeService.UpdateEmployee(employee);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("John Doe", result.Name);
        }

        [Test]
        public async Task DeleteEmployee_ExistingId_ReturnsTrue()
        {
            // Arrange
            _employeeRepositoryMock.Setup(repo => repo.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _employeeService.DeleteEmployee(1);

            // Assert
            Assert.IsTrue(result);
        }
    }
}