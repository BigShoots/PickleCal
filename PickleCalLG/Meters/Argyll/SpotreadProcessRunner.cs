using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PickleCalLG.Meters;

namespace PickleCalLG.Meters.Argyll
{
    public sealed class SpotreadProcessRunner
    {
        private readonly SpotreadLocator _locator;

        public SpotreadProcessRunner(SpotreadLocator locator)
        {
            _locator = locator;
        }

        public async Task<SpotreadMeasurement> RunMeasurementAsync(SpotreadRunParameters parameters, CancellationToken cancellationToken)
        {
            var executable = await _locator.FindAsync(cancellationToken) ?? throw new InvalidOperationException("spotread executable was not found. Install ArgyllCMS and ensure spotread is on PATH or set ARGYLLCMS_ROOT.");

            string logFile = Path.Combine(Path.GetTempPath(), $"spotread-log-{Guid.NewGuid():N}.tsv");
            try
            {
                var args = BuildArguments(parameters, logFile);
                var result = await RunProcessAsync(executable, args, cancellationToken);
                if (result.ExitCode != 0)
                {
                    throw new InvalidOperationException($"spotread exited with code {result.ExitCode}: {result.StandardError}".Trim());
                }

                if (!File.Exists(logFile))
                {
                    throw new InvalidOperationException("spotread did not produce a log file.");
                }

                var measurement = await ParseLogFileAsync(logFile, cancellationToken);
                return measurement;
            }
            finally
            {
                TryDelete(logFile);
            }
        }

        private static string BuildArguments(SpotreadRunParameters parameters, string logFile)
        {
            var builder = new StringBuilder();

            builder.Append("-O ");

            switch (parameters.Mode)
            {
                case MeterMeasurementMode.Display:
                    builder.Append("-e ");
                    break;
                case MeterMeasurementMode.Contact:
                    builder.Append("-p ");
                    break;
                case MeterMeasurementMode.Ambient:
                    builder.Append("-a ");
                    break;
            }

            if (parameters.HighResolution)
            {
                builder.Append("-H ");
            }

            if (parameters.UseAveraging)
            {
                builder.Append("-Y a ");
            }

            if (parameters.SkipInitialCalibration)
            {
                builder.Append("-N ");
            }

            if (!string.IsNullOrWhiteSpace(parameters.DisplayType))
            {
                builder.Append("-y \"");
                builder.Append(parameters.DisplayType);
                builder.Append("\" ");
            }

            if (!string.IsNullOrWhiteSpace(parameters.CorrectionFile))
            {
                builder.Append("-X \"");
                builder.Append(parameters.CorrectionFile);
                builder.Append("\" ");
            }

            if (!string.IsNullOrWhiteSpace(parameters.SpectralSampleFile))
            {
                builder.Append("-X \"");
                builder.Append(parameters.SpectralSampleFile);
                builder.Append("\" ");
            }

            if (!string.IsNullOrWhiteSpace(parameters.ExtraArguments))
            {
                builder.Append(parameters.ExtraArguments);
                builder.Append(' ');
            }

            builder.Append('"');
            builder.Append(logFile.Replace("\"", "\\\""));
            builder.Append('"');

            return builder.ToString();
        }

        private static async Task<SpotreadMeasurement> ParseLogFileAsync(string logFile, CancellationToken cancellationToken)
        {
            var lines = await File.ReadAllLinesAsync(logFile, cancellationToken);
            if (lines.Length < 2)
            {
                throw new InvalidOperationException("spotread log file did not contain any measurement lines.");
            }

            string headerLine = lines[0].Trim();
            string valueLine = lines[^1].Trim();
            if (string.IsNullOrEmpty(valueLine))
            {
                valueLine = lines.SkipLast(1).Last().Trim();
            }

            var headers = headerLine.Split('\t');
            var values = valueLine.Split('\t');

            if (headers.Length != values.Length)
            {
                throw new InvalidOperationException("spotread log header and value counts do not match.");
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                map[headers[i]] = values[i];
            }

            double x = GetDouble(map, "XYZ_X");
            double y = GetDouble(map, "XYZ_Y");
            double z = GetDouble(map, "XYZ_Z");
            double luminance = TryGetDouble(map, "cd/m2") ?? y;
            double? cct = TryGetDouble(map, "CCT");
            double? deltaE = TryGetDouble(map, "deltaE00") ?? TryGetDouble(map, "DeltaE");

            return new SpotreadMeasurement(DateTime.UtcNow, x, y, z, luminance, cct, deltaE);
        }

        private static double GetDouble(Dictionary<string, string> map, string key)
        {
            if (!TryGetDouble(map, key, out var value))
            {
                throw new InvalidOperationException($"spotread log is missing column '{key}'.");
            }
            return value;
        }

        private static double? TryGetDouble(Dictionary<string, string> map, string key)
        {
            return TryGetDouble(map, key, out var value) ? value : null;
        }

        private static bool TryGetDouble(Dictionary<string, string> map, string key, out double value)
        {
            if (map.TryGetValue(key, out var text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            value = 0;
            return false;
        }

        private static async Task<ProcessResult> RunProcessAsync(string executable, string arguments, CancellationToken cancellationToken)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();
            var stdOutTask = new TaskCompletionSource<bool>();
            var stdErrTask = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null)
                {
                    stdOutTask.TrySetResult(true);
                }
                else
                {
                    stdOut.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null)
                {
                    stdErrTask.TrySetResult(true);
                }
                else
                {
                    stdErr.AppendLine(e.Data);
                }
            };

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start spotread process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(() => process.WaitForExit(), cancellationToken);
            await Task.WhenAll(stdOutTask.Task, stdErrTask.Task);

            return new ProcessResult(process.ExitCode, stdOut.ToString(), stdErr.ToString());
        }

        private static void TryDelete(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // ignore cleanup errors
            }
        }
    }

    internal sealed class ProcessResult
    {
        public ProcessResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
    }
}
