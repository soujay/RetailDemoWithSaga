using NServiceBus;
namespace Messages;

public class DeliveryConfirmed : IMessage
{
    public string OrderId { get; set; }
}

public class DeliveryConfirmationFailed : IEvent
{
    public string OrderId { get; set; }
    public string ShippingProvider { get; set; }
}