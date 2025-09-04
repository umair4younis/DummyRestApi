using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using DummyRestApiExample.Authentication;
using DummyRestApiExample.Models;

namespace DummyRestApiExample.Tests.Authentication
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _authService = new AuthService(_userRepositoryMock.Object);
        }

        [Fact]
        public async Task Authenticate_ValidUser_ReturnsToken()
        {
            // Arrange
            var username = "testuser";
            var password = "testpassword";
            var user = new User { Username = username, Password = password };
            _userRepositoryMock.Setup(repo => repo.GetUserByUsernameAndPassword(username, password))
                               .ReturnsAsync(user);

            // Act
            var result = await _authService.Authenticate(username, password);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<string>(result);
        }

        [Fact]
        public async Task Authenticate_InvalidUser_ReturnsNull()
        {
            // Arrange
            var username = "invaliduser";
            var password = "invalidpassword";
            _userRepositoryMock.Setup(repo => repo.GetUserByUsernameAndPassword(username, password))
                               .ReturnsAsync((User)null);

            // Act
            var result = await _authService.Authenticate(username, password);

            // Assert
            Assert.Null(result);
        }
    }
}