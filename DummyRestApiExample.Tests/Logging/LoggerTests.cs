using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DummyRestApiExample.Tests.Logging
{
    public class LoggerTests
    {
        private readonly Mock<ILogger<Logger>> _mockLogger;
        private readonly Logger _logger;

        public LoggerTests()
        {
            _mockLogger = new Mock<ILogger<Logger>>();
            _logger = new Logger(_mockLogger.Object);
        }

        [Fact]
        public void LogInformation_ShouldLogInformationMessage()
        {
            // Arrange
            var message = "This is an information message.";

            // Act
            _logger.LogInformation(message);

            // Assert
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void LogError_ShouldLogErrorMessage()
        {
            // Arrange
            var message = "This is an error message.";
            var exception = new System.Exception("Test exception");

            // Act
            _logger.LogError(exception, message);

            // Assert
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
    }
}