using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BiMaDock
{
    internal static class ReleaseVersion
    {
        private static readonly Regex VersionPattern = new(
            @"^[vV]?(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:\.(?<revision>\d+))?(?:-Build(?<build>\d+))?$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static bool TryParse(string? value, out Version version)
        {
            version = new Version(0, 0, 0, 0);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim().Split('+')[0];
            Match match = VersionPattern.Match(normalized);
            if (!match.Success ||
                !int.TryParse(match.Groups["major"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int major) ||
                !int.TryParse(match.Groups["minor"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int minor) ||
                !int.TryParse(match.Groups["patch"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int patch))
            {
                return false;
            }

            string revisionText = match.Groups["build"].Success
                ? match.Groups["build"].Value
                : match.Groups["revision"].Value;
            int revision = 0;
            if (revisionText.Length > 0 &&
                !int.TryParse(revisionText, NumberStyles.None, CultureInfo.InvariantCulture, out revision))
            {
                return false;
            }

            version = new Version(major, minor, patch, revision);
            return true;
        }

        public static bool IsNewer(string currentVersion, string candidateVersion) =>
            TryParse(currentVersion, out Version current) &&
            TryParse(candidateVersion, out Version candidate) &&
            candidate > current;
    }
}
