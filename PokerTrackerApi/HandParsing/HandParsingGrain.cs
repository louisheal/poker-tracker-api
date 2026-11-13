using DotNetCore.CAP;
using PokerTrackerApi.Domain.HandHistories;
using PokerTrackerApi.Domain.Messages;
using PokerTrackerApi.HandParsing.Repository;

namespace PokerTrackerApi.HandParsing;

public interface IHandParsingGrain : IGrainWithStringKey
{
    Task ParseHand(string rawHand, CancellationToken cancellationToken);
}

public class HandParsingGrain(
    IHandHistoryParser handHistoryParser,
    IHandHistoryRepository repository,
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

        await repository.SaveHandHistory(handHistory, cancellationToken);

        await capPublisher.PublishAsync(
            PokerTrackerNames.HandHistoryUpdatesTopic,
            new HandHistoryParsed(handHistory.HandId, handHistory),
            cancellationToken: cancellationToken
        );
    }
}
