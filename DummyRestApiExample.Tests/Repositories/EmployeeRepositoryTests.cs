using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DummyRestApiExample.Tests.Repositories
{
    public class EmployeeRepositoryTests
    {
        private readonly Mock<DbSet<Employee>> _mockSet;
        private readonly Mock<AppDbContext> _mockContext;
        private readonly EmployeeRepository _repository;

        public EmployeeRepositoryTests()
        {
            _mockSet = new Mock<DbSet<Employee>>();
            _mockContext = new Mock<AppDbContext>();
            _mockContext.Setup(m => m.Employees).Returns(_mockSet.Object);
            _repository = new EmployeeRepository(_mockContext.Object);
        }

        [Fact]
        public void GetAllEmployees_ReturnsAllEmployees()
        {
            var employees = new List<Employee>
            {
                new Employee { Id = 1, Name = "John Doe", Salary = 50000, Age = 30 },
                new Employee { Id = 2, Name = "Jane Doe", Salary = 60000, Age = 25 }
            }.AsQueryable();

            _mockSet.As<IQueryable<Employee>>().Setup(m => m.Provider).Returns(employees.Provider);
            _mockSet.As<IQueryable<Employee>>().Setup(m => m.Expression).Returns(employees.Expression);
            _mockSet.As<IQueryable<Employee>>().Setup(m => m.ElementType).Returns(employees.ElementType);
            _mockSet.As<IQueryable<Employee>>().Setup(m => m.GetEnumerator()).Returns(employees.GetEnumerator());

            var result = _repository.GetAllEmployees();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetEmployeeById_ReturnsCorrectEmployee()
        {
            var employee = new Employee { Id = 1, Name = "John Doe", Salary = 50000, Age = 30 };
            _mockSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns(employee);

            var result = _repository.GetEmployeeById(1);

            Assert.Equal(employee, result);
        }

        [Fact]
        public void AddEmployee_AddsEmployee()
        {
            var employee = new Employee { Id = 3, Name = "New Employee", Salary = 70000, Age = 28 };

            _repository.AddEmployee(employee);

            _mockSet.Verify(m => m.Add(It.IsAny<Employee>()), Times.Once);
            _mockContext.Verify(m => m.SaveChanges(), Times.Once);
        }

        [Fact]
        public void UpdateEmployee_UpdatesEmployee()
        {
            var employee = new Employee { Id = 1, Name = "John Doe", Salary = 50000, Age = 30 };

            _repository.UpdateEmployee(employee);

            _mockSet.Verify(m => m.Update(It.IsAny<Employee>()), Times.Once);
            _mockContext.Verify(m => m.SaveChanges(), Times.Once);
        }

        [Fact]
        public void DeleteEmployee_DeletesEmployee()
        {
            var employee = new Employee { Id = 1, Name = "John Doe", Salary = 50000, Age = 30 };

            _repository.DeleteEmployee(employee);

            _mockSet.Verify(m => m.Remove(It.IsAny<Employee>()), Times.Once);
            _mockContext.Verify(m => m.SaveChanges(), Times.Once);
        }
    }
}