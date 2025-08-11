using api.Controllers.Base;
using application.Commands.Users;
using application.Queries.Users;
using domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api.Controllers;

[Route("api/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user and automatically create an account
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterUserResult>>> RegisterUser(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Registering new user with email {Email}", command.Email);
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("User registered successfully with email {Email}", command.Email);

            return CreatedResponse(result, "User registered and account created successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to register user with email {Email}", command.Email);
            return BadRequestResponse<RegisterUserResult>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user with email {Email}", command.Email);
            return InternalServerErrorResponse<RegisterUserResult>("An error occurred while registering the user");
        }
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResult>>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Login attempt for email {Email}", command.Email);
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("User logged in successfully with email {Email}", command.Email);

            return SuccessResponse(result, "Login successful");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Failed login attempt for email {Email}", command.Email);
            return UnauthorizedResponse<LoginResult>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email {Email}", command.Email);
            return InternalServerErrorResponse<LoginResult>("An error occurred during login");
        }
    }

    /// <summary>
    /// Get current user details
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDetailsDto>>> GetCurrentUser(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid or missing user ID in claims");
                return UnauthorizedResponse<UserDetailsDto>("Invalid user token");
            }

            var query = new GetUserDetailsQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(result, "User details retrieved successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User not found: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return NotFoundResponse<UserDetailsDto>("User not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user details for user: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return InternalServerErrorResponse<UserDetailsDto>("An error occurred while retrieving user details");
        }
    }

    /// <summary>
    /// Get current user's accounts
    /// </summary>
    [HttpGet("me/accounts")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserAccountsDto>>> GetCurrentUserAccounts(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid or missing user ID in claims");
                return UnauthorizedResponse<UserAccountsDto>("Invalid user token");
            }

            var query = new GetUserAccountsQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(result, "User accounts retrieved successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User not found: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return NotFoundResponse<UserAccountsDto>("User not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user accounts for user: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return InternalServerErrorResponse<UserAccountsDto>("An error occurred while retrieving user accounts");
        }
    }

    /// <summary>
    /// Logout user (invalidate token on client side)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public ActionResult<ApiResponse> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User logged out: {UserId}", userIdClaim);
            
            // For JWT, actual logout happens on client side by removing the token
            // This endpoint serves as a confirmation and logging purpose
            return SuccessResponse("Logout successful. Please remove the token from client storage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return InternalServerErrorResponse("An error occurred during logout");
        }
    }
}