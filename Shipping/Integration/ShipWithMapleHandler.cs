using Messages;
using Microsoft.Extensions.Logging;
using NServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shipping.Integration;

#region ShipWithMapleHandler

class ShipWithMapleHandler(ILogger<ShipWithMapleHandler> logger) : IHandleMessages<ShipWithMaple>
{
    const int MaximumTimeMapleMightRespond = 60;

    public async Task Handle(ShipWithMaple message, IMessageHandlerContext context)
    {
        var waitingTime = Random.Shared.Next(MaximumTimeMapleMightRespond);

        logger.LogInformation($"ShipWithMapleHandler: Delaying Order [{message.OrderId}] {waitingTime} seconds.");

        await Task.Delay(waitingTime * 1000, CancellationToken.None);

        await context.Reply(new ShipmentAcceptedByMaple() { OrderId = message.OrderId });
    }

    //public Task Handle(YetAnotherMessage message, IMessageHandlerContext context)
    //{
    //    return Task.CompletedTask;
    //}
}

#endregion