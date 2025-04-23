using System;
using System.Threading.Tasks;
using Messages;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Shipping;

class ShipOrderWorkflow(ILogger<ShipOrderWorkflow> logger) :
    Saga<ShipOrderWorkflowData>,
    IAmStartedByMessages<ShipOrder>,
    IHandleMessages<ShipmentAcceptedByMaple>,
    IHandleMessages<ShipmentAcceptedByAlpine>,
    IHandleTimeouts<ShipOrderWorkflow.ShippingEscalation>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ShipOrderWorkflowData> mapper)
    {
        mapper.MapSaga(saga => saga.OrderId)
            .ToMessage<ShipOrder>(message => message.OrderId);
    }

    public async Task Handle(ShipOrder message, IMessageHandlerContext context)
    {
        logger.LogInformation("ShipOrderWorkflow for Order [{.OrderId}] - Trying Maple first.", Data.OrderId);

        // Execute order to ship with Maple
        await context.Send(new ShipWithMaple() { OrderId = Data.OrderId });
        await context.Send(new YetAnotherMessage() { Data = "1 while handling started by ShipOrder " });
        await context.Send(new YetAnotherMessage() { Data = "2 while handling started by ShipOrder " });

        // Add timeout to escalate if Maple did not ship in time.
        await RequestTimeout(context, TimeSpan.FromSeconds(20), new ShippingEscalation());
        await RequestTimeout(context, TimeSpan.FromSeconds(10), new ShippingEscalation());
    }

    public Task Handle(ShipmentAcceptedByMaple message, IMessageHandlerContext context)
    {
        if (!Data.ShipmentOrderSentToAlpine)
        {
            logger.LogInformation("Order [{OrderId}] - Successfully shipped with Maple", Data.OrderId);

            Data.ShipmentAcceptedByMaple = true;

            MarkAsComplete();
        }

        return Task.CompletedTask;
    }

    public Task Handle(ShipmentAcceptedByAlpine message, IMessageHandlerContext context)
    {
        logger.LogInformation("Order [{OrderId}] - Successfully shipped with Alpine", Data.OrderId);

        Data.ShipmentAcceptedByAlpine = true;

        MarkAsComplete();

        return Task.CompletedTask;
    }

    public async Task Timeout(ShippingEscalation timeout, IMessageHandlerContext context)
    {
        if (!Data.ShipmentAcceptedByMaple)
        {
            if (!Data.ShipmentOrderSentToAlpine)
            {
                logger.LogInformation("Order [{OrderId}] - No answer from Maple, let's try Alpine.", Data.OrderId);
                Data.ShipmentOrderSentToAlpine = true;
                await context.Send(new ShipWithAlpine() { OrderId = Data.OrderId });
                await context.Send(new YetAnotherMessage() { Data = "1 from timeout" });
                await context.Send(new YetAnotherMessage() { Data = "2 from timeout" });
                await RequestTimeout(context, TimeSpan.FromSeconds(20), new ShippingEscalation());
            }
            else if (!Data.ShipmentAcceptedByAlpine) // No response from Maple nor Alpine
            {
                logger.LogWarning("Order [{.OrderId}] - No answer from Maple/Alpine. We need to escalate!", Data.OrderId);

                // escalate to Warehouse Manager!
                await context.Publish<ShipmentFailed>();

                MarkAsComplete();
            }
        }
    }

    internal class ShippingEscalation
    {
    }
}

public class ShipOrderWorkflowData : ContainSagaData
{
    public string OrderId { get; set; }
    public bool ShipmentAcceptedByMaple { get; set; }
    public bool ShipmentOrderSentToAlpine { get; set; }
    public bool ShipmentAcceptedByAlpine { get; set; }
}