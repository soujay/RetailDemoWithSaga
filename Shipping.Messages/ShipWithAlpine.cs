using NServiceBus;

namespace Messages;

public class ShipWithAlpine : ICommand
{
    public string OrderId { get; set; }
}

public class YetAnotherMessage: IMessage
{
    public string Data { get; set; }
}