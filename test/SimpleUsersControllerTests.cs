using api.Controllers;
using application.Commands.Users;
using application.Queries.Users;
using domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace test.Controllers;

public class SimpleUsersControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly UsersController _controller;

    public SimpleUsersControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterUser_WithValidCommand_ShouldCallMediator()
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

        // Act
        await _controller.RegisterUser(command, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithValidCommand_ShouldCallMediator()
    {
        // Arrange
        var command = new LoginCommand("john.doe@example.com", "Password123!");

        // Act
        await _controller.Login(command, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidUserId_ShouldCallMediator()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId);

        // Act
        await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Send(It.Is<GetUserDetailsQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAccounts_WithValidUserId_ShouldCallMediator()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId);

        // Act
        await _controller.GetCurrentUserAccounts(CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Send(It.Is<GetUserAccountsQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Logout_ShouldReturnOkResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId);

        // Act
        var result = _controller.Logout();

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    private void SetupUserContext(Guid userId)
    {
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
    }
}