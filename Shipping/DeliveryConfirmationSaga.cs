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

    private async Task ShipmentAccepted(string orderId, string provider, IMessageHandlerContext context)
    {
        Data.OrderId = orderId;
        Data.ShippingProvider = provider;
        logger.LogInformation("Starting delivery tracking for Order [{OrderId}] with {Provider}", orderId, provider);
        await RequestTimeout(context, TimeSpan.FromHours(48), new DeliveryEscalation());
    }



    public Task Handle(ShipmentAccepted message, IMessageHandlerContext context)
    => ShipmentAccepted(message.OrderId, message.ShippingProvider, context);
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
}