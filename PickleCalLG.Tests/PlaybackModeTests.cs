using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PickleCalLG;
using PickleCalLG.Meters;
using PickleCalLG.Meters.Sequences;
using Xunit;

namespace PickleCalLG.Tests
{
    public class PlaybackModeTests
    {
        [Fact]
        public async Task ManualMode_LogsManualInstruction()
        {
            await RunOnStaAsync(async () =>
            {
                using var form = new MainForm();
                SetField(form, "_patternMode", PatternPlaybackMode.Manual);

                var step = new MeasurementStep("50% Gray", 50d, MeterMeasurementMode.Display, TimeSpan.Zero, useAveraging: false);

                await InvokeHandleSequencePatternAsync(form, step);

                string lastLog = GetLastLogEntry(form);
                Assert.Contains("Set pattern manually", lastLog);
            });
        }

        [Fact]
        public async Task PGeneratorMode_EmitsPatternAndLogs()
        {
            await RunOnStaAsync(async () =>
            {
                using var form = new MainForm();

                var server = new PGenServer();
                string? patternJson = null;
                server.OnPatternChange += message => patternJson = message;

                SetField(form, "_patternMode", PatternPlaybackMode.PGenerator);
                SetField(form, "_pgenServer", server);

                var step = new MeasurementStep("50% Gray", 50d, MeterMeasurementMode.Display, TimeSpan.Zero, useAveraging: false);

                await InvokeHandleSequencePatternAsync(form, step);

                string lastLog = GetLastLogEntry(form);
                Assert.NotNull(patternJson);
                Assert.Contains("fullfield", patternJson, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("PGenerator full-field", lastLog);
            });
        }

        [Fact]
        public async Task LgTvMode_AttemptsToastAndLogsFailureWhenNotConnected()
        {
            await RunOnStaAsync(async () =>
            {
                using var form = new MainForm();

                var controller = new LgTvController("127.0.0.1", useSecure: false);
                SetPrivateProperty(controller, "IsConnected", true);
                SetField(form, "_patternMode", PatternPlaybackMode.LgTv);
                SetField(form, "_tvController", controller);

                var step = new MeasurementStep("50% Gray", 50d, MeterMeasurementMode.Display, TimeSpan.Zero, useAveraging: false);

                await InvokeHandleSequencePatternAsync(form, step);

                string lastLog = GetLastLogEntry(form);
                Assert.Contains("TV pattern request failed", lastLog);
            });
        }

        private static async Task InvokeHandleSequencePatternAsync(MainForm form, MeasurementStep step)
        {
            MethodInfo? method = typeof(MainForm).GetMethod("HandleSequencePatternAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var task = method!.Invoke(form, new object?[] { step, CancellationToken.None }) as Task;
            Assert.NotNull(task);
            await task!;

            // allow pending UI messages to flush
            Application.DoEvents();
        }

        private static void SetField(object target, string fieldName, object? value)
        {
            FieldInfo? field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field!.SetValue(target, value);
        }

        private static void SetPrivateProperty(object target, string propertyName, object value)
        {
            PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property);
            MethodInfo? setter = property!.GetSetMethod(true);
            Assert.NotNull(setter);
            setter!.Invoke(target, new[] { value });
        }

        private static string GetLastLogEntry(MainForm form)
        {
            FieldInfo? listField = typeof(MainForm).GetField("lstMeasurementLog", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(listField);
            var listBox = (ListBox?)listField!.GetValue(form);
            Assert.NotNull(listBox);
            Assert.True(listBox!.Items.Count > 0, "Expected at least one log entry.");
            return listBox.Items.Cast<object>().Last().ToString() ?? string.Empty;
        }

        private static Task RunOnStaAsync(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<object?>();

            Thread thread = new Thread(() =>
            {
                try
                {
                    action().GetAwaiter().GetResult();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            })
            {
                IsBackground = true
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }
    }
}
