using NServiceBus;

namespace Shipping;

public class ShipmentFailed : IEvent
{
    public string OrderId { get; set; }
}