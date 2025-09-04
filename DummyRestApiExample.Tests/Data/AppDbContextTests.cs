using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace DummyRestApiExample.Tests.Data
{
    [TestClass]
    public class AppDbContextTests
    {
        private AppDbContext _context;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new AppDbContext(options);
        }

        [TestMethod]
        public async Task Can_Add_Employee()
        {
            var employee = new Employee { Name = "John Doe", Salary = 50000, Age = 30 };
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            var retrievedEmployee = await _context.Employees.FindAsync(employee.Id);
            Assert.IsNotNull(retrievedEmployee);
            Assert.AreEqual("John Doe", retrievedEmployee.Name);
        }

        [TestMethod]
        public async Task Can_Get_Employees()
        {
            var employee1 = new Employee { Name = "John Doe", Salary = 50000, Age = 30 };
            var employee2 = new Employee { Name = "Jane Doe", Salary = 60000, Age = 25 };
            await _context.Employees.AddRangeAsync(employee1, employee2);
            await _context.SaveChangesAsync();

            var employees = await _context.Employees.ToListAsync();
            Assert.AreEqual(2, employees.Count);
        }

        [TestMethod]
        public async Task Can_Delete_Employee()
        {
            var employee = new Employee { Name = "John Doe", Salary = 50000, Age = 30 };
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            var retrievedEmployee = await _context.Employees.FindAsync(employee.Id);
            Assert.IsNull(retrievedEmployee);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}