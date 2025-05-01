using Messages;
using Microsoft.Extensions.Logging;
using NServiceBus;
using System;
using System.Threading.Tasks;


namespace Shipping;

class DeliveryConfirmationSaga(ILogger<DeliveryConfirmationSaga> logger) :
   Saga<DeliveryConfirmationData>,
   IAmStartedByMessages<ShipmentAccepted>,
   IHandleMessages<DeliveryConfirmed>,
   IHandleTimeouts<DeliveryConfirmationSaga.DeliveryEscalation>

{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DeliveryConfirmationData> mapper)
    {
        mapper.MapSaga(saga => saga.OrderId)
            .ToMessage<ShipmentAccepted>(message => message.OrderId)
            .ToMessage<DeliveryConfirmed>(message => message.OrderId);
    }

    private async Task ShipmentAccepted(string orderId, string provider, string? shippingAddress, IMessageHandlerContext context)
    {
        Data.OrderId = orderId;
        Data.ShippingProvider = provider;
        Data.ShippingProvider = shippingAddress;
        logger.LogInformation("Starting delivery tracking for Order [{OrderId}] with {Provider}", orderId, provider);
        await RequestTimeout(context, TimeSpan.FromHours(48), new DeliveryEscalation());
    }



    public Task Handle(ShipmentAccepted message, IMessageHandlerContext context)
    => ShipmentAccepted(message.OrderId, message.ShippingProvider, null, context);
    public Task Handle(DeliveryConfirmed message, IMessageHandlerContext context)
    {
        logger.LogInformation("Order [{OrderId}] - Delivery confirmed by {Provider}", Data.OrderId, Data.ShippingProvider);
        MarkAsComplete();
        return Task.CompletedTask;
    }

    public async Task Timeout(DeliveryEscalation deliveryTimeout, IMessageHandlerContext context)
    {
        logger.LogWarning("Order [{OrderId}] - Delivery confirmation timeout for {Provider}", Data.OrderId, Data.ShippingProvider);
        await context.Publish(new DeliveryConfirmationFailed { OrderId = Data.OrderId, ShippingProvider = Data.ShippingProvider });
        MarkAsComplete();
    }

    internal class DeliveryEscalation
    {
    }
}

public class DeliveryConfirmationData : ContainSagaData
{
    public string OrderId { get; set; }
    public string ShippingProvider { get; set; }
    public Address ProviderAddress { get; set; } = new();
}
public class Address
{
    public string AddressLine1 { get; set; } = "ABC default street";
    public string AddressLine2 { get; set; } = "#Unit 555";
    public string City { get; set; } = "ParticularCity";
    public string State { get; set; } = "CA";
    public string ZipCode { get; set; } = "92694";
}