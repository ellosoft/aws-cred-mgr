// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

public class TestCommandApp
{
    private readonly CommandApp _app;

    public TestCommandApp(IServiceProvider servicesProvider)
    {
        Console = new TestConsole().EmitAnsiSequences().Interactive().Width(int.MaxValue);

        _app = new CommandApp(new TestTypeRegistrar(servicesProvider));
        _app.Configure(config =>
        {
            config.PropagateExceptions();
            config.ConfigureConsole(Console);

            config.SetInterceptor(new CallbackCommandInterceptor((ctx, s) =>
            {
                Context = ctx;
                Settings = s;
            }));
        });

        // TODO: Remove this once all commands start using the IAnsiConsole interface
        AnsiConsole.Console = Console;
    }

    public CommandSettings Settings { get; set; } = null!;

    public CommandContext Context { get; set; } = null!;

    public TestConsole Console { get; }

    public void Configure(Action<IConfigurator> configure) => _app.Configure(configure);

    public (int exitCode, string output) Run(params string[] args)
    {
        var result = _app.Run(args);

        var output = Console.Output
            .NormalizeLineEndings()
            .TrimLines()
            .Trim();

        return (result, output);
    }
}
