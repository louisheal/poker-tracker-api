using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandHistories;

[ApiController]
[Route("api/handhistories")]
public class HandHistoryController : ControllerBase
{
    private readonly IHandHistorySummaryRepository _repository;
    private readonly IHandHistoryMapper _mapper;

    public HandHistoryController(IHandHistorySummaryRepository repository, IHandHistoryMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<HandHistoryDto[]> GetHandHistories(CancellationToken cancellationToken)
    {
        var handHistorySummaries = await _repository.GetHandHistorySummaries(cancellationToken);
        return handHistorySummaries
            .Select(hand =>
            {
                var holeCardsDto = _mapper.Map(hand.HoleCards);
                return new HandHistoryDto(hand.HandId, holeCardsDto);
            })
            .ToArray();
    }
}
