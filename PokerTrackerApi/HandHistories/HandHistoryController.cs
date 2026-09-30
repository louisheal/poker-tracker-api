using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandHistories;

[ApiController]
[Route("api/handhistories")]
public class HandHistoryController : ControllerBase
{
    private readonly IHandHistoryRepository _repository;

    public HandHistoryController(IHandHistoryRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<HandHistoryDto[]> GetHandHistories(CancellationToken cancellationToken)
    {
        var parsedHands = await _repository.GetParsedHands(cancellationToken);
        return parsedHands.Select(hand => new HandHistoryDto(hand.HandId)).ToArray();
    }
}