// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Reflection;
using System.Text.RegularExpressions;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Cli;

internal static partial class CliAttributeReader
{
    [GeneratedRegex("[^\\s]*\"[^\"]+\"[^\\s]*|[^\"\\s]+")]
    private static partial Regex ExampleRegex();

    public static string GetName(MemberInfo memberInfo)
    {
        var name = memberInfo.GetCustomAttribute<NameAttribute>();
        ArgumentNullException.ThrowIfNull(name);

        return name.Name;
    }

    public static void GetDescription(MemberInfo memberInfo, Action<string> descriptionAction)
    {
        var description = memberInfo.GetCustomAttribute<DescriptionAttribute>();

        if (description is not null)
            descriptionAction(description.Description);
    }

    public static void GetExamples(MemberInfo memberInfo, Action<string[]> exampleAction)
    {
        var examples = memberInfo.GetCustomAttribute<ExamplesAttribute>();

        if (examples is null)
            return;

        foreach (var example in examples.Examples)
        {
            var exampleSegments = ExampleRegex().Matches(example).Select(m => m.Value).ToArray();

            if (exampleSegments is { Length: > 0 })
                exampleAction(exampleSegments);
        }
    }

    public static void GetAliases(MemberInfo memberInfo, Action<string> aliasAction)
    {
        var aliases = memberInfo.GetCustomAttribute<AliasAttribute>();

        if (aliases is null)
            return;

        foreach (var alias in aliases.Aliases)
        {
            if (!String.IsNullOrWhiteSpace(alias))
            {
                aliasAction(alias);
            }
        }
    }
}
