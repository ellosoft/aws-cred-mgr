# AWS Credential Manager (aws-cred-mgr)

AWS Credential Manager (`aws-cred-mgr`) is a command-line interface (CLI) tool designed to simplify the management of AWS RDS credentials, specifically for users authenticating with Okta. This utility offers a seamless experience for configuring Okta authentication, creating and managing AWS credential profiles, and handling RDS tokens effectively.

## Features

-   **Okta Authentication**: Easily setup Okta authentication for you user
-   **Credential Management**: Create and list AWS credentials, manage profiles with ease.
-   **RDS Token Management**: Obtain RDS passwords for your databases securely.

## Installation

To install aws-cred-mgr, download the latest version from the [GitHub Release](https://github.com/ellosoft/aws-cred-mgr/releases) page

## Usage

### Okta Configuration

```plaintext
aws-cred-mgr okta setup
```

#### Examples

-   Simply run `aws-cred-mgr okta setup` to use interactive mode.
-   Set up with domain and username: `aws-cred-mgr okta setup -d https://xyz.okta.com -u john --mfa push`
-   Set up a specific profile with all options: `aws-cred-mgr okta setup xyz_profile -d https://xyz.okta.com -u john --mfa push`

### Credential Management

```plaintext
aws-cred-mgr cred [COMMAND]
```

#### Subcommands

-   `list` (alias `ls`): List all saved credential profiles.
-   `new`: Create a new credential profile.

#### Examples

-   List credentials: `aws-cred-mgr cred ls`
-   Create a new credential profile named `prod`: `aws-cred-mgr cred new prod`

### RDS Token Management

```plaintext
aws-cred-mgr rds pwd
```

#### Examples

-   Get RDS password : `aws-cred-mgr rds pwd`
-   Get RDS password for `prod_db`: `aws-cred-mgr rds pwd prod_db`
-   Get RDS password with all options: `aws-cred-mgr rds pwd -h localhost -p 5432 -u john`

## Security Note for Windows Users

On Windows systems, `aws-cred-mgr` securely stores your Okta credentials using the Data Protection API (DPAPI). This ensures that your sensitive information is encrypted and can only be accessed by your user account on your computer.

MacOS support is still under development

## Full Configuration Example

You can specify additional variables, templates, credentials, and RDS configurations in the YAML file `aws_cred_mgr.yml` located in your home folder

```yaml
variables:
    rds_username: my.user
    default_pwd_lifetime: 15
    # any variable can be specified here
---
authentication:
    okta:
        default: # default Okta profile name, additional profiles can also be created
            okta_domain: https://xyz.okta.com/
            preferred_mfa_type: push
            auth_type: classic

credentials:
    my_aws_dev_account: # credentials can be interactively created with `aws-cred-mgr cred new`
        role_arn: arn:aws:iam::123:role:/my_aws_role_arn
        aws_profile: default
        okta_app_url: https://xyz.okta.com/home/amazon_aws/abc/272
        okta_profile: default
    ...

templates:
    rds:
        orders_db: # templates can be created to simply configurations
            hostname: rds-hostname.aws.endpoint
            port: 5432
            username: ${rds_username} # variable usage
            ttl: ${default_pwd_lifetime}
            region: us-east-2
        ...

environments:
    dev:
        credential: my_aws_dev_account
        rds:
            orders_db:
                hostname: dev.endpoint # overrides the template value
                template: orders_db
            products_db:
                hostname: rds-hostname.aws.endpoint
                port: 5432
                username: ${rds_username}
                ttl: ${default_pwd_lifetime}
                region: us-east-2
                credential: products_db # override env credential
    test:
        credential: my_aws_dev_account
        rds:
            orders_db:
                hostname: test.endpoint
                template: orders_db
    ...
```

## Support

If you encounter any issues or require assistance, please open an issue on the project's GitHub page.

## Contribution

Contributions are welcome! Please fork the repository and submit a pull request with your changes or improvements.

> Note: I know I don't have unit tests, I'm working on it...

## Code of Conduct

[![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.1-4baaaa.svg)](code_of_conduct.md)

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community. For more information see the [Code of Conduct](CODE_OF_CONDUCT.md).

## License

This project is licensed under the terms of the MIT license.
