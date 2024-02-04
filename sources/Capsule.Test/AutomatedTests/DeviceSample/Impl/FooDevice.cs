using Capsule.Attribution;

using Microsoft.Extensions.Logging;

namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

[Capsule(InterfaceName = nameof(IDevice), GenerateInterface = false)]
public class FooDevice : IDevice, CapsuleFeature.IInitializer, CapsuleFeature.ITimers
{
    // ReSharper disable once NotAccessedField.Local
    private readonly IStateTracker _stateTracker;

    private readonly ILogger<FooDevice> _logger;

    public FooDevice(string id, IStateTracker stateTracker, ILogger<FooDevice> logger)
    {
        Id = id;
        _stateTracker = stateTracker;
        _logger = logger;
    }

    public ICapsuleTimerService? Timers { private get; set; }

    [Expose(Synchronization = CapsuleSynchronization.PassThrough)]
    public string Id { get; }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Foo device {Id} initialized", Id);
    }

    [Expose]
    public async Task<bool> SetOutputAsync(string id, bool value)
    {
        Timers!.StartNew(TimeSpan.FromSeconds(10), async () => await ReadIosAsync());
        return true;
    }

    [Expose]
    public async Task<IReadOnlyDictionary<string, bool>> ReadIosAsync()
    {
        return new Dictionary<string, bool>();
    }

    [Expose]
    public async ValueTask DisposeAsync()
    {
    }
}
