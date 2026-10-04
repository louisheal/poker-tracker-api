using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandHistories;

[ApiController]
[Route("api/handhistories")]
public class HandHistoryController : ControllerBase
{
    private readonly IHandHistoryRepository _repository;
    private readonly IHandHistoryMapper _mapper;

    public HandHistoryController(IHandHistoryRepository repository, IHandHistoryMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<HandHistoryDto[]> GetHandHistories(CancellationToken cancellationToken)
    {
        var handHistories = await _repository.GetHandHistoriesAsync(cancellationToken);
        return handHistories
            .Select(hand =>
            {
                var holeCardsDto = _mapper.Map(hand.HeroHoleCards);
                return new HandHistoryDto(hand.HandId, holeCardsDto);
            })
            .ToArray();
    }
}
