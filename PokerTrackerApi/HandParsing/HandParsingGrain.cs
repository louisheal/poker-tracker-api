using DotNetCore.CAP;
using PokerTrackerApi.Domain.HandHistories;
using PokerTrackerApi.Domain.Messages;

namespace PokerTrackerApi.HandParsing;

public interface IHandParsingGrain : IGrainWithStringKey
{
    Task ParseHand(string rawHand, CancellationToken cancellationToken);
}

public class HandParsingGrain(
    IHandHistoryParser handHistoryParser,
    ICapPublisher capPublisher
) : Grain, IHandParsingGrain
{
    // TODO : accept a RawHandHistory record containing Id and Text as strings
    public async Task ParseHand(string handHistoryText, CancellationToken cancellationToken)
    {
        HandHistory handHistory;
        try
        {
            handHistory = handHistoryParser.ParseHandHistory(handHistoryText);
        }
        catch (FormatException)
        {
            // TODO : log
            return;
        }

        await capPublisher.PublishAsync(
            PokerTrackerNames.HandHistoriesTopic,
            new HandHistoryParsed(handHistory.HandId, handHistory),
            cancellationToken: cancellationToken
        );
    }
}
