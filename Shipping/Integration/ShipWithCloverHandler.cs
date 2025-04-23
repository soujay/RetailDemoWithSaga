using Messages;
using Microsoft.Extensions.Logging;
using NServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shipping.Integration;

#region ShipWithCloverHandler

class ShipWithCloverHandler(ILogger<ShipWithCloverHandler> logger) : IHandleMessages<ShipWithClover>
{
    const int MaximumTimeCloverMightRespond = 30;

    public async Task Handle(ShipWithClover message, IMessageHandlerContext context)
    {
        var waitingTime = Random.Shared.Next(MaximumTimeCloverMightRespond);

        logger.LogInformation($"ShipWithCloverHandler: Delaying Order [{message.OrderId}] {waitingTime} seconds.");

        await Task.Delay(waitingTime * 1000, CancellationToken.None);

        await context.Reply(new ShipmentAcceptedByClover() { OrderId = message.OrderId });
    }
}

#endregion