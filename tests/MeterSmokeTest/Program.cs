using System;
using System.Threading;
using System.Threading.Tasks;
using PickleCalLG.Meters;
using PickleCalLG.Meters.Argyll;
using PickleCalLG.Meters.Sequences;
using PickleCalLG.Meters.Simulation;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var manager = new MeterManager(
            new CompositeMeterDiscoveryService(
                new SimulatedMeterDiscoveryService(),
                new ArgyllSpotreadDiscoveryService()));

        await manager.RefreshMetersAsync(cts.Token);
        Console.WriteLine($"Discovered {manager.KnownMeters.Count} meter(s).");
        foreach (var meter in manager.KnownMeters)
        {
            Console.WriteLine($"- {meter.DisplayName} [{meter.Id}] Capabilities: {meter.Capabilities}");
        }

        if (manager.KnownMeters.Count == 0)
        {
            Console.WriteLine("No meters discovered. Exiting.");
            return 1;
        }

        var descriptor = manager.KnownMeters[0];
        Console.WriteLine($"Using meter: {descriptor.DisplayName}");

        if (!await manager.SelectMeterAsync(descriptor.Id, cts.Token))
        {
            Console.WriteLine("Failed to select meter.");
            return 2;
        }

        var options = new MeterConnectOptions
        {
            PreferredMode = MeterMeasurementMode.Display,
            UseHighResolution = false
        };

        await manager.ConnectActiveMeterAsync(options, cts.Token);
        Console.WriteLine("Meter connected.");

        var calibration = await manager.CalibrateAsync(new MeterCalibrationRequest(MeterMeasurementMode.Display, true), cts.Token);
        Console.WriteLine(calibration?.Success == true ? "Calibration succeeded." : $"Calibration failed: {calibration?.Message}");

        var runner = new MeasurementQueueRunner(manager);
        runner.SequenceStarted += seq => Console.WriteLine($"Starting sequence: {seq.Name}");
        runner.StepStarted += step => Console.WriteLine($" - {step.Name} ({step.TargetIre:F0} IRE)");
        runner.StepCompleted += (step, res) =>
        {
            if (res?.Success == true && res.Reading != null)
            {
                var (x, y) = res.Reading.Chromaticity;
                Console.WriteLine($"   -> Y={res.Reading.Luminance:F2} cd/m^2, x={x:F4}, y={y:F4}");
            }
            else
            {
                Console.WriteLine($"   -> Failed: {res?.Message ?? "Unknown"}");
            }
        };

        await runner.RunAsync(MeasurementSequences.Grayscale10Point(), cts.Token);
        await runner.RunAsync(MeasurementSequences.PrimarySecondarySweep(), cts.Token);

        await manager.DisconnectAsync(cts.Token);
        Console.WriteLine("Meter disconnected.");

        return 0;
    }
}
