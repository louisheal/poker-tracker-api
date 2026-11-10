using DotNetCore.CAP;
using PokerTrackerApi.Domain.Messages;

namespace PokerTrackerApi.HandParsing;

public class RawHandUpdateReceiver : ICapSubscribe
{
    private readonly IGrainFactory _grainFactory;

    public RawHandUpdateReceiver(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    [CapSubscribe(PokerTrackerNames.RawHandUpdatesTopic, Group = "hand-parsing")]
    public async Task HandleAsync(HandExtracted message, CancellationToken cancellationToken)
    {
        try
        {
            var grain = _grainFactory.GetGrain<IHandParsingGrain>(message.HandId);
            await grain.ParseHand(message.RawText, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(message.RawText);
        }
    }
}
