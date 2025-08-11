using application.Commands.Users;
using domain.Enums;
using FluentValidation.TestHelper;

namespace test.Validators;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator;

    public RegisterUserCommandValidatorTests()
    {
        _validator = new RegisterUserCommandValidator();
    }

    [Fact]
    public void Should_HaveError_When_FirstName_IsEmpty()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "",
            "Doe",
            "john.doe@example.com",
            "+1234567890",
            "StrongPassword123!",
            AccountType.Savings,
            "1234"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("First name is required");
    }

    [Fact]
    public void Should_HaveError_When_FirstName_ExceedsMaxLength()
    {
        // Arrange
        var command = new RegisterUserCommand(
            new string('a', 51), // 51 characters
            "Doe",
            "john.doe@example.com",
            "+1234567890",
            "StrongPassword123!",
            AccountType.Savings,
            "1234"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("First name cannot exceed 50 characters");
    }

    [Fact]
    public void Should_HaveError_When_FirstName_ContainsInvalidCharacters()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "John123",
            "Doe",
            "john.doe@example.com",
            "+1234567890",
            "StrongPassword123!",
            AccountType.Savings,
            "1234"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("First name can only contain letters and spaces");
    }

    [Fact]
    public void Should_HaveError_When_Email_IsInvalid()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "John",
            "Doe",
            "invalid-email",
            "+1234567890",
            "StrongPassword123!",
            AccountType.Savings,
            "1234"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email must be a valid email address");
    }

    [Fact]
    public void Should_HaveError_When_PhoneNumber_IsInvalid()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "John",
            "Doe",
            "john.doe@example.com",
            "invalid-phone",
            "StrongPassword123!",
            AccountType.Savings,
            "1234"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
              .WithErrorMessage("Phone number must be a valid international format");
    }

    [Fact]
    public void Should_HaveError_When_Password_IsWeak()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "John",
            "Doe",
            "john.doe@example.com",
            "+1234567890",
            "weak",
            AccountType.Savings,
            "1234"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_PIN_IsNotFourDigits()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "John",
            "Doe",
            "john.doe@example.com",
            "+1234567890",
            "StrongPassword123!",
            AccountType.Savings,
            "12" // Too short
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PIN)
              .WithErrorMessage("PIN must be between 4 and 6 characters");
    }

    [Fact]
    public void Should_HaveError_When_PIN_ContainsNonDigits()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "John",
            "Doe",
            "john.doe@example.com",
            "+1234567890",
            "StrongPassword123!",
            AccountType.Savings,
            "12ab"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PIN)
              .WithErrorMessage("PIN must contain only digits");
    }

    [Fact]
    public void Should_NotHaveError_When_AllFieldsAreValid()
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
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(AccountType.Savings)]
    [InlineData(AccountType.Current)]
    [InlineData(AccountType.FixedDeposit)]
    public void Should_NotHaveError_When_AccountType_IsValid(AccountType accountType)
    {
        // Arrange
        var command = new RegisterUserCommand(
            "John",
            "Doe",
            "john.doe@example.com",
            "+1234567890",
            "StrongPassword123!",
            accountType,
            "1234"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.InitialAccountType);
    }
}