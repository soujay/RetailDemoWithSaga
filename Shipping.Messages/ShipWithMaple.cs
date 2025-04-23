using NServiceBus;

namespace Messages;

public class ShipWithMaple : ICommand
{
    public string OrderId { get; set; }
}
public class ShipmentAcceptedByMaple : IMessage
{
    public string OrderId { get; set; }
}