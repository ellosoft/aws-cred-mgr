// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Infrastructure;

public record SemanticVersion : IComparable<SemanticVersion>
{
    private Version MainVersion { get; }

    private string? PreReleaseLabel { get; }

    private int PreReleaseNumber { get; }

    private string? BuildData { get; }

    public bool IsPreRelease => PreReleaseLabel is not null;

    public SemanticVersion(string versionValue)
    {
        if (!TryParse(versionValue, out var mainVersion, out var preReleaseLabel, out var preReleaseNumber, out var buildData))
            throw new ArgumentException($"'{versionValue}' is not a valid SemanticVersion");

        MainVersion = mainVersion;
        PreReleaseLabel = preReleaseLabel;
        PreReleaseNumber = preReleaseNumber;
        BuildData = buildData;
    }

    private SemanticVersion(Version mainVersion, string? preReleaseLabel, int preReleaseNumber, string? buildData)
    {
        MainVersion = mainVersion;
        PreReleaseLabel = preReleaseLabel;
        PreReleaseNumber = preReleaseNumber;
        BuildData = buildData;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
            return 1;

        var mainVersionComparison = MainVersion.CompareTo(other.MainVersion);

        if (mainVersionComparison != 0)
            return mainVersionComparison;

        var isPreRelease = !string.IsNullOrEmpty(PreReleaseLabel);
        var otherIsPreRelease = !string.IsNullOrEmpty(other.PreReleaseLabel);

        if (isPreRelease && !otherIsPreRelease)
            return -1;

        if (!isPreRelease && otherIsPreRelease)
            return 1;

        var preReleaseLabelComparison = StringComparer.OrdinalIgnoreCase.Compare(PreReleaseLabel, other.PreReleaseLabel);

        if (preReleaseLabelComparison != 0)
            return preReleaseLabelComparison;

        return PreReleaseNumber.CompareTo(other.PreReleaseNumber);
    }

    public override string ToString()
    {
        var result = MainVersion.ToString();

        if (PreReleaseLabel is not null)
        {
            result += $"-{PreReleaseLabel}";

            if (PreReleaseNumber != 0)
                result += $".{PreReleaseNumber}";
        }

        if (BuildData is not null)
            result += $"+{BuildData}";

        return result;
    }

    public static bool operator <(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) < 0;

    public static bool operator >(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) > 0;

    public static bool operator <=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) >= 0;

    public static bool TryParse(string? versionValue, [NotNullWhen(true)] out SemanticVersion? version)
    {
        version = null;

        if (!TryParse(versionValue, out var mainVersion, out var preReleaseLabel, out var preReleaseNumber, out var buildData))
            return false;

        version = new SemanticVersion(mainVersion, preReleaseLabel, preReleaseNumber, buildData);

        return true;
    }

    private static bool TryParse(string? versionValue,
        [NotNullWhen(true)] out Version? mainVersion, out string? preReleaseLabel, out int preReleaseNumber, out string? buildData)
    {
        mainVersion = null;
        preReleaseLabel = null;
        preReleaseNumber = 0;
        buildData = null;

        if (versionValue is null)
            return false;

        var buildDataIndex = versionValue.IndexOf('+');

        if (buildDataIndex > 0)
        {
            buildData = versionValue[(buildDataIndex + 1)..];
            versionValue = versionValue[..buildDataIndex];
        }

        var preReleaseIndex = versionValue.IndexOf('-');

        if (preReleaseIndex > 0)
        {
            preReleaseLabel = versionValue[(preReleaseIndex + 1)..];
            versionValue = versionValue[..preReleaseIndex];

            var preReleaseNumberIndex = preReleaseLabel.IndexOf('.');

            if (preReleaseNumberIndex > 0)
            {
                if (!int.TryParse(preReleaseLabel[(preReleaseNumberIndex + 1)..], out preReleaseNumber))
                    return false;

                preReleaseLabel = preReleaseLabel[..preReleaseNumberIndex];
            }
        }

        return Version.TryParse(versionValue, out mainVersion);
    }
}
