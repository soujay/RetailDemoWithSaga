using NServiceBus;

namespace Shipping;

public class ShipmentAccepted : IEvent
{
    public string OrderId { get; set; }
    public string ShippingProvider { get; set; }
}