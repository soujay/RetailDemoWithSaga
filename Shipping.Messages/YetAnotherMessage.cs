using NServiceBus;

namespace Messages;

public class YetAnotherMessage : IMessage
{
    public string Data { get; set; }
}