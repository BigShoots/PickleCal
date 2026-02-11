using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters.Argyll
{
    public sealed class SpotreadLocator
    {
        private readonly string[] _candidateNames =
        {
            "spotread.exe",
            "spotread"
        };

        public async Task<string?> FindAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var path in EnumerateCandidates())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (File.Exists(path))
                {
                    return await Task.FromResult(Path.GetFullPath(path));
                }
            }

            return null;
        }

        private static string[] GetSearchDirectories()
        {
            var locations = Environment.GetEnvironmentVariable("ARGYLLCMS_ROOT");
            if (!string.IsNullOrWhiteSpace(locations))
            {
                return locations.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(path => path.Trim())
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => Path.IsPathRooted(path) ? path : Path.GetFullPath(path))
                    .Concat(GetPathDirectories())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            return GetPathDirectories();
        }

        private static string[] GetPathDirectories()
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            return pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(path => path.Trim())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
        }

        private IEnumerable<string> EnumerateCandidates()
        {
            foreach (var directory in GetSearchDirectories())
            {
                foreach (var name in _candidateNames)
                {
                    yield return Path.Combine(directory, name);
                }
            }
        }
    }
}
