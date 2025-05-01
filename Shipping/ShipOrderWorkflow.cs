using Messages;
using Microsoft.Extensions.Logging;
using NServiceBus;
using System;
using System.Threading.Tasks;

namespace Shipping;

class ShipOrderWorkflow(ILogger<ShipOrderWorkflow> logger) :
    Saga<ShipOrderWorkflowData>,
    IAmStartedByMessages<ShipOrder>,
    IHandleMessages<ShipmentAcceptedByMaple>,
    IHandleMessages<ShipmentAcceptedByAlpine>,
    IHandleMessages<ShipmentAcceptedByClover>,
    IHandleTimeouts<ShipOrderWorkflow.ShippingEscalation>
{
    private const int CountOfYetAnotherMessageType = 5;
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
        for (int i = 0; i <= CountOfYetAnotherMessageType; i++)
        {
            await context.Send(new YetAnotherMessage() { Data = $"{i} while handling started by ShipOrder" });
        }

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
        }
        return context.Publish(new ShipmentAccepted
        {
            OrderId = Data.OrderId,
            ShippingProvider = "Maple",
        });
    }

    public Task Handle(ShipmentAcceptedByAlpine message, IMessageHandlerContext context)
    {
        logger.LogInformation("Order [{OrderId}] - Successfully shipped with Alpine", Data.OrderId);

        Data.ShipmentAcceptedByAlpine = true;

        return context.Publish(new ShipmentAccepted
        {
            OrderId = Data.OrderId,
            ShippingProvider = "Alpine",

        });
    }

    public Task Handle(ShipmentAcceptedByClover message, IMessageHandlerContext context)
    {
        logger.LogInformation("Order [{OrderId}] - Successfully shipped with Clover", Data.OrderId);
        Data.ShipmentAcceptedByClover = true;
        Data.ShipmentContainerColor = new[] { "Green", "Maroon" };
        return context.Publish(new ShipmentAccepted
        {
            OrderId = Data.OrderId,
            ShippingProvider = "Clover"
        });
    }
    public Task Handle(YetAnotherMessage message, IMessageHandlerContext context)
    {
        logger.LogInformation("Handling YetAnotherMessage with data: {Data}", Data.OrderId);
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
                Data.ShipmentContainerColor = new[] { "Red", "Yellow" };
                await context.Send(new ShipWithAlpine() { OrderId = Data.OrderId });
                for (int i = 0; i <= CountOfYetAnotherMessageType; i++)
                {
                    await context.Send(new YetAnotherMessage() { Data = $"{i} from  Alpine timeout" });
                }


                await RequestTimeout(context, TimeSpan.FromSeconds(20), new ShippingEscalation());
            }
            else if (!Data.ShipmentAcceptedByAlpine && !Data.ShipmentOrderSentToClover)
            {
                logger.LogInformation("Order [{OrderId}] - No answer from Alpine, trying Clover.", Data.OrderId);
                Data.ShipmentOrderSentToClover = true;
                Data.ShipmentContainerColor = new[] { "", "Pink" };
                await context.Send(new ShipWithClover() { OrderId = Data.OrderId });
                for (int i = 0; i <= CountOfYetAnotherMessageType; i++)
                {
                    await context.Send(new YetAnotherMessage() { Data = $"{i} from  Clover timeout" });
                }
                await RequestTimeout(context, TimeSpan.FromMinutes(5), new ShippingEscalation());
            }
            else if (!Data.ShipmentAcceptedByAlpine && !Data.ShipmentAcceptedByClover)
            {
                logger.LogWarning("Order [{OrderId}] - No answer from any shipping provider. Escalating!", Data.OrderId);
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
    public string[] ShipmentContainerColor { get; set; } = ["White", "Black"];
    public bool ShipmentAcceptedByAlpine { get; set; }
    public bool ShipmentOrderSentToClover { get; set; }
    public bool ShipmentAcceptedByClover { get; set; }
}