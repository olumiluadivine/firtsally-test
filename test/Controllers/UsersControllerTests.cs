using api.Controllers;
using application.Commands.Users;
using application.Queries.Users;
using domain.Common;
using domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace test.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterUser_WithValidCommand_ReturnsCreatedResponse()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "John",
            "Doe",
            "john.doe@example.com",
            "+1234567890",
            "StrongPassword123!",
            AccountType.Savings,
            "1234"
        );

        var expectedResult = new RegisterUserResult(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow.AddDays(1)
            );

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RegisterUser(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<RegisterUserResult>>().Subject;

        response.Success.Should().BeTrue();
        response.Message.Should().Be("User registered and account created successfully");
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task RegisterUser_WithInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "John",
            "Doe",
            "existing@example.com",
            "+1234567890",
            "StrongPassword123!",
            AccountType.Savings,
            "1234"
        );

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("User with this email already exists"));

        // Act
        var result = await _controller.RegisterUser(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<RegisterUserResult>>().Subject;

        response.Success.Should().BeFalse();
        response.Message.Should().Be("User with this email already exists");
        response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task RegisterUser_WithUnhandledException_ReturnsInternalServerError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "John",
            "Doe",
            "john.doe@example.com",
            "+1234567890",
            "StrongPassword123!",
            AccountType.Savings,
            "1234"
        );

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.RegisterUser(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        actionResult.StatusCode.Should().Be(500);

        var response = actionResult.Value.Should().BeOfType<ApiResponse<RegisterUserResult>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("An error occurred while registering the user");
        response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessResponse()
    {
        // Arrange
        var command = new LoginCommand("john.doe@example.com", "Password123!");
        var userProfile = new UserProfileDto(
            Guid.NewGuid(),
            "John Doe",
            "john.doe@example.com",
            "+1234567890",
            DateTime.UtcNow,
            true
        );

        var expectedResult = new LoginResult(
            "jwt_token_here",
            DateTime.UtcNow.AddMonths(1),
            userProfile
        );

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Login(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<LoginResult>>().Subject;

        response.Success.Should().BeTrue();
        response.Message.Should().Be("Login successful");
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("john.doe@example.com", "WrongPassword");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password"));

        // Act
        var result = await _controller.Login(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<LoginResult>>().Subject;

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Invalid email or password");
        response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var command = new LoginCommand("john.doe@example.com", "Password123!");

        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Login(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        actionResult.StatusCode.Should().Be(500);

        var response = actionResult.Value.Should().BeOfType<ApiResponse<LoginResult>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("An error occurred during login");
        response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedResult = new UserDetailsDto(
            userId,
            "John",
            "Doe",
            "john.doe@example.com",
            "+1234567890",
            DateTime.UtcNow.AddDays(-30),
            true,
            true,
            DateTime.UtcNow.AddHours(-1)
        );

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mediatorMock.Setup(m => m.Send(It.Is<GetUserDetailsQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<UserDetailsDto>>().Subject;

        response.Success.Should().BeTrue();
        response.Message.Should().Be("User details retrieved successfully");
        response.Data.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-guid")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<UserDetailsDto>>().Subject;

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Invalid user token");
        response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mediatorMock.Setup(m => m.Send(It.Is<GetUserDetailsQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("User not found"));

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<UserDetailsDto>>().Subject;

        response.Success.Should().BeFalse();
        response.Message.Should().Be("User not found");
        response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetCurrentUserAccounts_WithValidUser_ReturnsAccounts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountDto = new UserAccountDto(
            Guid.NewGuid(),
            "1234567890",
            AccountType.Savings,
            1000.00m,
            "NGN",
            true,
            DateTime.UtcNow
        );

        var expectedResult = new UserAccountsDto(
            userId,
            "John Doe",
            new[] { accountDto }
        );

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mediatorMock.Setup(m => m.Send(It.Is<GetUserAccountsQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetCurrentUserAccounts(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<UserAccountsDto>>().Subject;

        response.Success.Should().BeTrue();
        response.Message.Should().Be("User accounts retrieved successfully");
        response.Data.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public void Logout_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = _controller.Logout();

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse>().Subject;

        response.Success.Should().BeTrue();
        response.Message.Should().Be("Logout successful. Please remove the token from client storage.");
        response.StatusCode.Should().Be(200);
    }
}