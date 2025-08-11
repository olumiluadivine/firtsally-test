using api.Controllers.Base;
using application.Commands.Accounts;
using application.Queries.Accounts;
using domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class AccountsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IMediator mediator, ILogger<AccountsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new bank account
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateAccountResult>>> CreateAccount(
        [FromBody] CreateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new account for user {UserId}", command.UserId);
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Account created successfully. Account Number: {AccountNumber}", result.AccountNumber);

            return CreatedResponse(result, "Bank account created successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create account for user {UserId}", command.UserId);
            return BadRequestResponse<CreateAccountResult>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account for user {UserId}", command.UserId);
            return InternalServerErrorResponse<CreateAccountResult>("An error occurred while creating the account");
        }
    }

    /// <summary>
    /// Get account details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountDetailsDto>>> GetAccount(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAccountDetailsQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(result, "Account details retrieved successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Account not found: {AccountId}", id);
            return NotFoundResponse<AccountDetailsDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account: {AccountId}", id);
            return InternalServerErrorResponse<AccountDetailsDto>("An error occurred while retrieving account details");
        }
    }

    /// <summary>
    /// Get account balance
    /// </summary>
    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<ApiResponse<object>>> GetAccountBalance(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAccountDetailsQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            var balanceInfo = new
            {
                accountId = result.Id,
                accountNumber = result.AccountNumber,
                balance = result.Balance,
                currency = result.Currency
            };

            return SuccessResponse<object>(balanceInfo, "Account balance retrieved successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Account not found: {AccountId}", id);
            return NotFoundResponse<object>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account balance: {AccountId}", id);
            return InternalServerErrorResponse<object>("An error occurred while retrieving account balance");
        }
    }

    /// <summary>
    /// Change account PIN securely
    /// </summary>
    [HttpPut("{id:guid}/change-pin")]
    public async Task<ActionResult<ApiResponse<ChangePINResult>>> ChangePIN(
        Guid id,
        [FromBody] ChangePINCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure the account ID in the route matches the command
            if (id != command.AccountId)
            {
                return BadRequestResponse<ChangePINResult>("Account ID in route does not match the request body");
            }

            _logger.LogInformation("Changing PIN for account {AccountId}", id);
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("PIN changed successfully for account {AccountId}", id);

            return SuccessResponse(result, "PIN changed successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized PIN change attempt for account {AccountId}", id);
            return UnauthorizedResponse<ChangePINResult>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to change PIN for account {AccountId}", id);
            return BadRequestResponse<ChangePINResult>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing PIN for account {AccountId}", id);
            return InternalServerErrorResponse<ChangePINResult>("An error occurred while changing the PIN");
        }
    }
}