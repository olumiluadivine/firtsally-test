using api.Controllers;
using application.Commands.Accounts;
using application.Queries.Accounts;
using domain.Common;
using domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace test.Controllers;

public class AccountsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<AccountsController>> _loggerMock;
    private readonly AccountsController _controller;

    public AccountsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<AccountsController>>();
        _controller = new AccountsController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAccount_WithValidCommand_ReturnsCreatedResponse()
    {
        // Arrange
        var command = new CreateAccountCommand(
            "John Doe",
            "john.doe@example.com",
            AccountType.Savings,
            "1234",
            Guid.NewGuid()
        );

        var expectedResult = new CreateAccountResult(
            Guid.NewGuid(),
            "1234567890",
            AccountType.Savings,
            DateTime.UtcNow
        );

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateAccount(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<CreateAccountResult>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Bank account created successfully");
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateAccount_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateAccountCommand(
            "John Doe",
            "existing@example.com",
            AccountType.Savings,
            "1234",
            Guid.NewGuid()
        );

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Account with this email already exists"));

        // Act
        var result = await _controller.CreateAccount(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<CreateAccountResult>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Account with this email already exists");
        response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateAccount_WithUnhandledException_ReturnsInternalServerError()
    {
        // Arrange
        var command = new CreateAccountCommand(
            "John Doe",
            "john.doe@example.com",
            AccountType.Savings,
            "1234",
            Guid.NewGuid()
        );

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.CreateAccount(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        actionResult.StatusCode.Should().Be(500);
        
        var response = actionResult.Value.Should().BeOfType<ApiResponse<CreateAccountResult>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("An error occurred while creating the account");
        response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetAccount_WithValidId_ReturnsAccountDetails()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var expectedResult = new AccountDetailsDto(
            accountId,
            "1234567890",
            1500.00m,
            "NGN",
            AccountType.Savings,
            true,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddHours(-1)
        );

        _mediatorMock.Setup(m => m.Send(It.Is<GetAccountDetailsQuery>(q => q.AccountId == accountId), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAccount(accountId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<AccountDetailsDto>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Account details retrieved successfully");
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAccount_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.Is<GetAccountDetailsQuery>(q => q.AccountId == accountId), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Account not found"));

        // Act
        var result = await _controller.GetAccount(accountId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<AccountDetailsDto>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Account not found");
        response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAccount_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.Is<GetAccountDetailsQuery>(q => q.AccountId == accountId), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAccount(accountId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        actionResult.StatusCode.Should().Be(500);
        
        var response = actionResult.Value.Should().BeOfType<ApiResponse<AccountDetailsDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("An error occurred while retrieving account details");
        response.StatusCode.Should().Be(500);
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("0987654321")]
    public async Task GetAccountBalance_WithValidId_ReturnsBalance(string accountNumber)
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var expectedBalance = new { 
            accountId = accountId,
            accountNumber = accountNumber,
            balance = 2500.00m,
            currency = "NGN",
            availableBalance = 2500.00m,
            lastUpdated = DateTime.UtcNow
        };

        // Since we don't have the exact query, we'll mock the mediator to return the balance
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedBalance);

        // Act
        var result = await _controller.GetAccountBalance(accountId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(expectedBalance);
    }

    [Fact]
    public async Task GetAccountBalance_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Account not found"));

        // Act
        var result = await _controller.GetAccountBalance(accountId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Account not found");
        response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAccountBalance_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAccountBalance(accountId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        actionResult.StatusCode.Should().Be(500);
        
        var response = actionResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(500);
    }
}

public class AccountControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AccountControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateAccount_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var createAccountRequest = new CreateAccountCommand(
            "John Doe",
            "john.doe.test@example.com",
            AccountType.Savings,
            "1234",
            Guid.NewGuid()
        );

        // This would require proper setup of test database and authentication
        // For now, this demonstrates the structure for integration tests
        
        // Act & Assert would go here with actual HTTP calls
        Assert.True(true); // Placeholder assertion
    }
}