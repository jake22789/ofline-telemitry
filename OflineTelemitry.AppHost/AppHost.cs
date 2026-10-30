var builder = DistributedApplication.CreateBuilder(args);

var app = builder.AddMauiProject("oflinetelemitry", @"..\ofline telemitry\ofline telemitry.csproj");

// Send telemetry over HTTP, not gRPC. With gRPC, the exporter drops batches when the phone can't look up
// the dashboard's address (airplane mode) instead of saving them to disk to retry.
app.AddWindowsDevice()
    .WithOtlpExporter(OtlpProtocol.HttpProtobuf);

// Start from the dashboard when the emulator is running. The tunnel carries telemetry from the emulator to this PC.
app.AddAndroidEmulator()
    .WithOtlpExporter(OtlpProtocol.HttpProtobuf)
    .WithOtlpDevTunnel()
    .WithExplicitStart();

builder.Build().Run();
