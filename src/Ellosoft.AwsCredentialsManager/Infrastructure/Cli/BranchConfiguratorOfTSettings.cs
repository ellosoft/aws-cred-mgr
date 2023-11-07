// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Cli;

public class BranchConfigurator<TBranchSettings> : IBranchConfigurator<TBranchSettings>
    where TBranchSettings : CommandSettings
{
    private readonly IConfigurator<TBranchSettings> _configurator;
    private readonly List<string> _hierarchy;

    public BranchConfigurator(IConfigurator<TBranchSettings> configurator, string branchName, List<string> hierarchy)
    {
        _configurator = configurator;

        _hierarchy = hierarchy;
        _hierarchy.Add(branchName);
    }

    public IBranchConfigurator<TBranchSettings> AddCommand<TCommand>() where TCommand : class, ICommandLimiter<TBranchSettings>
    {
        CommandConfiguratorExtensions.ConfigureCommand<TCommand>(_configurator.AddCommand<TCommand>, _hierarchy);

        return this;
    }

    public ICommandConfigurator AddCommand<TCommand>(string name) where TCommand : class, ICommandLimiter<TBranchSettings>
    {
        return _configurator.AddCommand<TCommand>(name);
    }

    public IBranchConfigurator<TBranchSettings> AddBranch<TBranch, TDerivedSettings>(Action<IBranchConfigurator<TDerivedSettings>> branchConfigAction)
        where TBranch : class
        where TDerivedSettings : TBranchSettings
    {
        BranchConfiguratorExtensions.ConfigureBranch<TBranch, TDerivedSettings>(_configurator.AddBranch, branchConfigAction, _hierarchy);

        return this;
    }

    public IBranchConfigurator<TBranchSettings> AddBranch<TBranch>(Action<IBranchConfigurator<TBranchSettings>> branchConfigAction)
        where TBranch : class
    {
        return AddBranch<TBranch, TBranchSettings>(branchConfigAction);
    }

    public IBranchConfigurator<TBranchSettings> SetDefaultCommand<TCommand>() where TCommand : class, ICommandLimiter<TBranchSettings>
    {
        _configurator.SetDefaultCommand<TCommand>();

        return this;
    }
}

public interface IBranchConfigurator<in TBranchSettings> where TBranchSettings : CommandSettings
{
    IBranchConfigurator<TBranchSettings> AddCommand<TCommand>() where TCommand : class, ICommandLimiter<TBranchSettings>;

    ICommandConfigurator AddCommand<TCommand>(string name) where TCommand : class, ICommandLimiter<TBranchSettings>;

    IBranchConfigurator<TBranchSettings> AddBranch<TBranch, TDerivedSettings>(Action<IBranchConfigurator<TDerivedSettings>> branchConfigAction)
        where TBranch : class
        where TDerivedSettings : TBranchSettings;

    IBranchConfigurator<TBranchSettings> AddBranch<TBranch>(Action<IBranchConfigurator<TBranchSettings>> branchConfigAction)
        where TBranch : class;

    IBranchConfigurator<TBranchSettings> SetDefaultCommand<TCommand>() where TCommand : class, ICommandLimiter<TBranchSettings>;
}
