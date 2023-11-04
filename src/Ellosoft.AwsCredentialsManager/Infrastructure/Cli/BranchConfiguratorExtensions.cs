// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Reflection;
using static Ellosoft.AwsCredentialsManager.Infrastructure.Cli.CliAttributeReader;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Cli;

public static class BranchConfiguratorExtensions
{
    /// <summary>
    ///     Adds a CLI branch and configure it using its CLI attributes.
    /// </summary>
    /// <typeparam name="TBranch"></typeparam>
    /// <param name="configurator"></param>
    /// <param name="branchConfigAction"></param>
    /// <returns></returns>
    public static IConfigurator AddBranch<TBranch>(this IConfigurator configurator, Action<IBranchConfigurator<CommandSettings>> branchConfigAction)
        where TBranch : class
    {
        return AddBranch<TBranch, CommandSettings>(configurator, branchConfigAction);
    }

    /// <summary>
    ///     Adds a CLI branch and configure it using its CLI attributes.
    /// </summary>
    /// <typeparam name="TBranch"></typeparam>
    /// <typeparam name="TSettings"></typeparam>
    /// <param name="configurator"></param>
    /// <param name="branchConfigAction"></param>
    /// <returns></returns>
    public static IConfigurator AddBranch<TBranch, TSettings>(this IConfigurator configurator, Action<IBranchConfigurator<TSettings>> branchConfigAction)
        where TSettings : CommandSettings
        where TBranch : class
    {
        ConfigureBranch<TBranch, TSettings>(configurator.AddBranch, branchConfigAction, new List<string>());

        return configurator;
    }

    internal static void ConfigureBranch<TBranch, TSettings>(Func<string, Action<IConfigurator<TSettings>>, IBranchConfigurator> branchConfiguratorFactory,
        Action<IBranchConfigurator<TSettings>> branchConfigAction, List<string> hierarchy)
        where TBranch : class
        where TSettings : CommandSettings
    {
        var branchType = typeof(TBranch);
        var branchName = GetName(branchType);

        var branchConfigurator = branchConfiguratorFactory(branchName, configurator =>
        {
            ConfigureBranchDescription(branchType, configurator);
            ConfigureBranchExamples(branchType, configurator);

            var childConfigurator = new BranchConfigurator<TSettings>(configurator, branchName, hierarchy);
            branchConfigAction(childConfigurator);
        });

        GetAliases(branchType, alias => branchConfigurator.WithAlias(alias));
    }

    private static void ConfigureBranchDescription<TSettings>(MemberInfo branchType, IConfigurator<TSettings> branchConfigurator)
        where TSettings : CommandSettings
    {
        GetDescription(branchType, branchConfigurator.SetDescription);
    }

    private static void ConfigureBranchExamples<TSettings>(MemberInfo branchType, IConfigurator<TSettings> branchConfigurator)
        where TSettings : CommandSettings
    {
        GetExamples(branchType, branchConfigurator.AddExample);
    }
}
