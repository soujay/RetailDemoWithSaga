using NServiceBus;
namespace Messages;

public class ShipWithClover : IMessage
{
    public string OrderId { get; set; }
}

public class ShipmentAcceptedByClover : IMessage
{
    public string OrderId { get; set; }
}