# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AWS Credential Manager (`aws-cred-mgr`) is a CLI tool for managing AWS credentials, particularly for users authenticating with Okta. It provides credential management, RDS token management, and configuration capabilities.

## Development Commands

### Build
```bash
dotnet build
```

### Test
```bash
dotnet test
```

### Run Tests with Specific Verbosity
```bash
dotnet test --verbosity normal
```

### Restore Dependencies
```bash
dotnet restore
```

### Run the Application
```bash
dotnet run --project src/Ellosoft.AwsCredentialsManager/Ellosoft.AwsCredentialsManager.csproj -- [arguments]
```

### Publish as Single File (for distribution)
```bash
dotnet publish src/Ellosoft.AwsCredentialsManager/Ellosoft.AwsCredentialsManager.csproj -c Release -r [runtime-identifier]
```
Common runtime identifiers: `osx-x64`, `osx-arm64`, `linux-x64`, `win-x64`

## Architecture Overview

### Core Components

1. **Command Structure** (`src/Ellosoft.AwsCredentialsManager/Commands/`)
   - Uses Spectre.Console.Cli for command-line interface
   - Organized into branches: AWS, Config, Credentials, Okta, RDS
   - Each command inherits from Spectre.Console command classes

2. **Service Layer** (`src/Ellosoft.AwsCredentialsManager/Services/`)
   - **AWS Services**: Credential and SAML authentication handling
   - **Okta Services**: Authentication flow, MFA handling (push, TOTP, etc.)
   - **Configuration**: YAML-based config management with variable substitution
   - **Security**: Platform-specific secure storage (Windows DPAPI, macOS Keychain)

3. **Platform-Specific Implementations**
   - Windows: Uses Data Protection API for secure storage
   - macOS: Uses native Keychain API via Objective-C interop
   - Platform detection via compile-time constants (MACOS, WINDOWS)

4. **Configuration System**
   - Primary config file: `~/aws_cred_mgr.yml`
   - Supports YAML and TOML formats
   - Variable substitution using `${variable_name}` syntax
   - Hierarchical structure: variables, authentication, credentials, templates, environments

## Key Technical Details

- **Framework**: .NET 8.0
- **Publishing**: Single-file, self-contained, trimmed executable
- **Testing Framework**: xUnit with FluentAssertions
- **Mocking**: NSubstitute
- **CLI Framework**: Spectre.Console.Cli
- **Logging**: Serilog with file sinks
- **Code Analysis**: SonarAnalyzer, enforced code style
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled

## Testing Approach

Tests are located in `test/Ellosoft.AwsCredentialsManager.Tests/`:
- Integration tests use ASP.NET Core TestHost for API mocking
- Fake APIs simulate Okta and AWS endpoints
- Test utilities include custom assertions and test sinks

## Security Considerations

- Credentials are stored using platform-specific secure storage
- Never log or expose sensitive information
- Okta passwords and MFA tokens are handled securely
- AWS credentials are written to standard AWS credentials file

## Configuration File Structure

The tool uses a YAML configuration file with these main sections:
- `variables`: Global variables for reuse
- `authentication.okta`: Okta authentication profiles
- `credentials`: AWS credential profiles
- `templates`: Reusable configuration templates
- `environments`: Environment-specific configurations