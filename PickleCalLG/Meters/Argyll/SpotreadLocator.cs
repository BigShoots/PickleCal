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
            var dirs = new List<string>();

            // 1. ARGYLLCMS_ROOT environment variable (highest priority)
            var argyllRoot = Environment.GetEnvironmentVariable("ARGYLLCMS_ROOT");
            if (!string.IsNullOrWhiteSpace(argyllRoot))
            {
                foreach (var seg in argyllRoot.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = seg.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                        dirs.Add(Path.IsPathRooted(trimmed) ? trimmed : Path.GetFullPath(trimmed));
                }
            }

            // 2. Common ArgyllCMS install locations on Windows
            if (OperatingSystem.IsWindows())
            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                // Standard ArgyllCMS installs
                AddIfExists(dirs, Path.Combine(programFiles, "ArgyllCMS", "bin"));
                AddIfExists(dirs, Path.Combine(programFilesX86, "ArgyllCMS", "bin"));
                AddIfExists(dirs, Path.Combine(programFiles, "Argyll_V2.3.1", "bin"));
                AddIfExists(dirs, Path.Combine(programFilesX86, "Argyll_V2.3.1", "bin"));

                // Try a wildcard version match: Argyll_V*
                TryAddVersionedDir(dirs, programFiles, "Argyll_V*");
                TryAddVersionedDir(dirs, programFilesX86, "Argyll_V*");

                // DisplayCAL bundled ArgyllCMS
                AddIfExists(dirs, Path.Combine(programFiles, "DisplayCAL", "ArgyllCMS", "bin"));
                AddIfExists(dirs, Path.Combine(programFilesX86, "DisplayCAL", "ArgyllCMS", "bin"));
                AddIfExists(dirs, Path.Combine(localAppData, "DisplayCAL", "ArgyllCMS", "bin"));
                AddIfExists(dirs, Path.Combine(appData, "DisplayCAL", "ArgyllCMS", "bin"));

                // Chocolatey installs
                AddIfExists(dirs, @"C:\ProgramData\chocolatey\lib\argyllcms\tools");
                AddIfExists(dirs, @"C:\ProgramData\chocolatey\bin");

                // Scoop installs
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                AddIfExists(dirs, Path.Combine(userProfile, "scoop", "apps", "argyllcms", "current", "bin"));

                // Application directory (bundled spotread)
                var appDir = AppContext.BaseDirectory;
                if (!string.IsNullOrEmpty(appDir))
                    dirs.Add(appDir);

                // PickleCal auto-downloaded ArgyllCMS cache
                var pickleCacheDir = ArgyllCmsManager.GetCacheDirectory();
                if (Directory.Exists(pickleCacheDir))
                    dirs.Add(pickleCacheDir);
            }

            // 3. System PATH
            dirs.AddRange(GetPathDirectories());

            return dirs.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static void AddIfExists(List<string> dirs, string path)
        {
            if (Directory.Exists(path))
                dirs.Add(path);
        }

        private static void TryAddVersionedDir(List<string> dirs, string parent, string pattern)
        {
            try
            {
                if (!Directory.Exists(parent)) return;
                foreach (var dir in Directory.GetDirectories(parent, pattern))
                {
                    var bin = Path.Combine(dir, "bin");
                    if (Directory.Exists(bin))
                        dirs.Add(bin);
                    else if (Directory.Exists(dir))
                        dirs.Add(dir);
                }
            }
            catch { }
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
