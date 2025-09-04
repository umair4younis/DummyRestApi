using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using DummyRestApiExample.Controllers;
using DummyRestApiExample.Models;
using DummyRestApiExample.Services;

namespace DummyRestApiExample.Tests.Controllers
{
    [TestFixture]
    public class EmployeeControllerTests
    {
        private EmployeeController _controller;
        private Mock<IEmployeeService> _mockService;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IEmployeeService>();
            _controller = new EmployeeController(_mockService.Object);
        }

        [Test]
        public async Task GetAllEmployees_ReturnsOkResult()
        {
            // Arrange
            var employees = new List<Employee>
            {
                new Employee { Id = 1, Name = "John Doe", Salary = 50000, Age = 30 },
                new Employee { Id = 2, Name = "Jane Doe", Salary = 60000, Age = 25 }
            };
            _mockService.Setup(service => service.GetAllEmployees()).ReturnsAsync(employees);

            // Act
            var result = await _controller.GetAllEmployees();

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(employees, okResult.Value);
        }

        [Test]
        public async Task GetEmployeeById_ExistingId_ReturnsOkResult()
        {
            // Arrange
            var employee = new Employee { Id = 1, Name = "John Doe", Salary = 50000, Age = 30 };
            _mockService.Setup(service => service.GetEmployeeById(1)).ReturnsAsync(employee);

            // Act
            var result = await _controller.GetEmployeeById(1);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(employee, okResult.Value);
        }

        [Test]
        public async Task GetEmployeeById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(service => service.GetEmployeeById(99)).ReturnsAsync((Employee)null);

            // Act
            var result = await _controller.GetEmployeeById(99);

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task CreateEmployee_ValidEmployee_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var employee = new Employee { Id = 3, Name = "New Employee", Salary = 70000, Age = 28 };
            _mockService.Setup(service => service.CreateEmployee(employee)).ReturnsAsync(employee);

            // Act
            var result = await _controller.CreateEmployee(employee);

            // Assert
            Assert.IsInstanceOf<CreatedAtActionResult>(result);
            var createdResult = result as CreatedAtActionResult;
            Assert.AreEqual(employee, createdResult.Value);
        }

        [Test]
        public async Task UpdateEmployee_ExistingEmployee_ReturnsNoContent()
        {
            // Arrange
            var employee = new Employee { Id = 1, Name = "Updated Employee", Salary = 80000, Age = 35 };
            _mockService.Setup(service => service.UpdateEmployee(employee)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateEmployee(employee.Id, employee);

            // Assert
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task DeleteEmployee_ExistingId_ReturnsNoContent()
        {
            // Arrange
            _mockService.Setup(service => service.DeleteEmployee(1)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteEmployee(1);

            // Assert
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task DeleteEmployee_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(service => service.DeleteEmployee(99)).ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.DeleteEmployee(99);

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(result);
        }
    }
}