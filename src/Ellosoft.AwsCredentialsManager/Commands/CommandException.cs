// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Commands;

public class CommandException : Exception
{
    public CommandException(string message) : base(message)
    {
    }
}
