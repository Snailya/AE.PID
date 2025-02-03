using AE.PID.Client.Core;

namespace AE.PID.Visio.UI.Design;

public class LocationKey(int id) : CompoundKeyBase
{
    public override int ComputedId { get; } = id;
}

public class TestKey(int a, int b) : CompoundKeyBase
{
    public override int ComputedId => a + b;
}