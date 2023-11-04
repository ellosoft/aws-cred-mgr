// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Reflection;
using static Ellosoft.AwsCredentialsManager.Infrastructure.Cli.CliAttributeReader;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Cli;

public static class CommandConfiguratorExtensions
{
    /// <summary>
    ///     Adds a CLI command and configure it using its CLI attributes.
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static IConfigurator AddCommand<TCommand>(this IConfigurator configurator)
        where TCommand : class, ICommand
    {
        ConfigureCommand<TCommand>(configurator.AddCommand<TCommand>);

        return configurator;
    }

    /// <summary>
    ///     Adds a CLI command and configure it using its CLI attributes.
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TSettings"></typeparam>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static IConfigurator<TSettings> AddCommand<TCommand, TSettings>(this IConfigurator<TSettings> configurator)
        where TCommand : class, ICommandLimiter<TSettings>
        where TSettings : CommandSettings
    {
        ConfigureCommand<TCommand>(configurator.AddCommand<TCommand>);

        return configurator;
    }

    internal static void ConfigureCommand<TCommand>(Func<string, ICommandConfigurator> commandConfiguratorFactory, IList<string>? ancestors = null)
        where TCommand : class, ICommand
    {
        var commandType = typeof(TCommand);
        var commandName = GetName(commandType);
        var commandConfigurator = commandConfiguratorFactory(commandName);

        ConfigureCommandDescription(commandType, commandConfigurator);
        ConfigureCommandExamples(commandType, commandConfigurator, ancestors);
        ConfigureCommandAliases(commandType, commandConfigurator);
    }

    private static void ConfigureCommandDescription(MemberInfo commandType, ICommandConfigurator commandConfigurator)
    {
        var getFirstAlias = commandType.GetCustomAttribute<AliasAttribute>()?.Aliases.FirstOrDefault();

        var aliasSuffix = getFirstAlias is not null ? $"[green italic] (alias: {getFirstAlias})[/]" : string.Empty;

        GetDescription(commandType, description => commandConfigurator.WithDescription(description + aliasSuffix));
    }

    private static void ConfigureCommandExamples(MemberInfo commandType, ICommandConfigurator commandConfigurator, IList<string>? ancestors)
    {
        var examplePrefix = ancestors is not null && ancestors.Any() ? ancestors : Enumerable.Empty<string>();

        GetExamples(commandType, example =>
        {
            var exampleCommand = examplePrefix.Concat(example).ToArray();
            commandConfigurator.WithExample(exampleCommand);
        });
    }

    private static void ConfigureCommandAliases(MemberInfo commandType, ICommandConfigurator commandConfigurator)
    {
        GetAliases(commandType, alias => commandConfigurator.WithAlias(alias));
    }
}
