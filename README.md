# 🏦 Modern Banking API

A comprehensive banking application built with .NET 8, implementing clean architecture principles with support for user management, account operations, transactions, and external payment integrations via Paystack.

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [API Documentation](#api-documentation)
- [Database Schema](#database-schema)
- [External Integrations](#external-integrations)
- [Security](#security)
- [Monitoring & Logging](#monitoring--logging)
- [Contributing](#contributing)
- [License](#license)

## 🎯 Overview

This is a modern banking API system that provides core banking functionalities including user registration, account management, fund transfers, deposits, withdrawals, and external payment processing. The system is built using Domain-Driven Design (DDD) principles with clean architecture, ensuring maintainability, testability, and scalability.

### Key Capabilities

- **User Management**: Registration, authentication, and profile management
- **Account Operations**: Multiple account types with secure PIN-based access
- **Internal Transfers**: Instant transfers between accounts within the system
- **External Transfers**: Bank-to-bank transfers via Paystack integration
- **Payment Processing**: Secure payment processing with webhook support
- **Transaction History**: Comprehensive transaction tracking and reporting
- **Real-time Notifications**: Background job processing for notifications

## 🏗️ Architecture

The project follows Clean Architecture principles with clear separation of concerns:

```
├── 📁 domain/          # Core business logic and entities
├── 📁 application/     # Use cases and application services
├── 📁 infrastructure/ # External concerns (database, APIs, caching)
└── 📁 api/           # Presentation layer (controllers, middleware)
```

### Architecture Layers

- **Domain Layer**: Contains business entities, value objects, enums, and domain services
- **Application Layer**: Implements use cases using CQRS pattern with MediatR
- **Infrastructure Layer**: Handles data persistence, external APIs, and cross-cutting concerns
- **API Layer**: Provides RESTful endpoints with proper validation and error handling

## ✨ Features

### 👤 User Management
- User registration with automatic account creation
- JWT-based authentication
- Secure password hashing with BCrypt
- User profile management

### 🏦 Account Operations
- Support for multiple account types (Savings, Current, Fixed Deposit)
- PIN-based account security
- Balance inquiries and account status management
- Account number generation

### 💸 Transaction Processing
- **Deposits**: 
  - Direct deposits for internal use
  - Paystack integration for external payment processing
  - Webhook confirmation for payment verification
- **Withdrawals**:
  - Simple withdrawals with PIN verification
  - Internal transfers between system accounts
  - External transfers to other banks via Paystack
- **Transfer Features**:
  - Real-time balance validation
  - Automatic reversal for failed transfers
  - Transaction reference tracking

### 📊 Reporting & Analytics
- Transaction history with filtering
- Monthly account statements
- Pagination support for large datasets
- Transaction status tracking

### 🔧 Administrative Features
- Health checks for system monitoring
- Background job processing with Hangfire
- Comprehensive logging with Serilog
- Redis-based caching for performance

## 🛠️ Technology Stack

### Backend Framework
- **.NET 8** - Latest .NET framework
- **ASP.NET Core 8** - Web API framework
- **Entity Framework Core 8** - ORM for data access
- **PostgreSQL** - Primary database
- **Redis** - Caching and session storage

### Libraries & Packages
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **Hangfire** - Background job processing
- **Serilog** - Structured logging
- **Paystack.NET** - Payment processing
- **BCrypt.NET** - Password hashing
- **JWT Bearer** - Authentication

### Development Tools
- **Swagger/OpenAPI** - API documentation
- **Health Checks** - Application monitoring

## ⚙️ Prerequisites

Before running this application, ensure you have:

- **.NET 8 SDK** or later
- **PostgreSQL** database server
- **Redis** server
- **Paystack Account** (for payment processing)
- **IDE**

## 🚀 Installation

### 1. Clone the Repository
```bash
git clone
cd banking-api
```

### 2. Restore Dependencies
```bash
dotnet restore
```

### 3. Set Up Configuration
Copy the example configuration and update with your settings:
```bash
cp api/appsettings.example.json api/appsettings.json
```

### 4. Update Database
```bash
cd api
dotnet ef database update
```

### 5. Run the Application
```bash
dotnet run --project api
```

The API will be available at:
- **HTTPS**: `https://localhost:7001`
- **HTTP**: `http://localhost:5000`
- **Swagger UI**: `https://localhost:7001/swagger`
- **Hangfire Dashboard**: `https://localhost:7001/bg`

## ⚙️ Configuration

Update your `appsettings.json` with the following configurations:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=BankingDB;Username=postgres;Password=your_password",
    "Redis": "localhost:6379"
  },
  "JWT": {
    "Key": "your-super-secret-jwt-key-min-32-characters",
    "Issuer": "BankingAPI",
    "ExpiryInDays": 30
  },
  "Paystack": {
    "SecretKey": "sk_test_your_paystack_secret_key",
    "PublicKey": "pk_test_your_paystack_public_key",
    "Uri": "https://api.paystack.co"
  },
  "Security": {
    "EncryptionKey": "your-32-character-encryption-key"
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/banking-api-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

## 📚 API Documentation

### Authentication Endpoints
- `POST /api/users/register` - Register new user
- `POST /api/users/login` - User authentication
- `POST /api/users/logout` - User logout
- `GET /api/users/me` - Get current user details
- `GET /api/users/me/accounts` - Get user accounts

### Transaction Endpoints
- `POST /api/transactions/deposit/initiate` - Initiate Paystack deposit
- `POST /api/transactions/deposit/confirm` - Confirm deposit payment
- `POST /api/transactions/deposit` - Direct deposit (admin)
- `POST /api/transactions/withdraw` - Internal withdrawal/transfer
- `POST /api/transactions/withdraw/external` - External bank transfer
- `POST /api/transactions/transfer` - Internal account transfer
- `GET /api/transactions/history/{accountId}` - Transaction history
- `GET /api/transactions/statement/{accountId}` - Monthly statement
- `GET /api/transactions/banks` - Available banks
- `POST /api/transactions/verify-account` - Verify recipient account

### System Endpoints
- `GET /api/test` - API health test
- `GET /health` - Application health check
- `POST /api/webhook/paystack` - Paystack webhook endpoint

## 🔌 External Integrations

### Paystack Integration

The application integrates with Paystack for:
- **Payment Processing**: Secure payment collection for deposits
- **Bank Transfers**: External bank-to-bank transfers
- **Account Verification**: Validate recipient bank account details
- **Webhook Handling**: Real-time payment status updates

#### Webhook Events Handled
- `transfer.success` - Transfer completion
- `transfer.failed` - Transfer failure (triggers automatic reversal)
- `transfer.reversed` - Paystack-initiated reversal

### Redis Integration
- **Caching**: Improves performance for frequently accessed data
- **Background Jobs**: Hangfire job queue management

## 🔒 Security

### Authentication & Authorization
- **JWT Tokens**: Stateless authentication with configurable expiry
- **Password Security**: BCrypt hashing with salt
- **PIN Protection**: Secure 4-digit PIN for transaction authorization

### Data Protection
- **Input Validation**: FluentValidation for all API inputs
- **SQL Injection Prevention**: Entity Framework parameterized queries
- **XSS Protection**: Automatic input sanitization
- **CORS Configuration**: Configurable cross-origin requests

### API Security
- **Webhook Verification**: HMAC signature validation for Paystack webhooks

### Best Practices Implemented
- Password complexity requirements
- Encryption for sensitive data at rest

## 📊 Monitoring & Logging

### Logging with Serilog
- **Structured Logging**: JSON-formatted logs for easy parsing
- **Multiple Sinks**: Console and file outputs
- **Log Levels**: Configurable logging levels
- **Request Logging**: Automatic HTTP request/response logging

### Health Checks
- Database connectivity monitoring
- Redis connection verification
- Custom health check endpoints

### Background Jobs with Hangfire
- **Job Dashboard**: Web-based job monitoring at `/bg`
- **Retry Policies**: Automatic retry for failed jobs
- **Job Scheduling**: Delayed and recurring job support

### Performance Monitoring
- Redis caching for improved response times
- Database query optimization with EF Core
- Async/await patterns for non-blocking operations