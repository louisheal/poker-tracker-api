using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandHistories;

[ApiController]
[Route("api/handhistories")]
public class HandHistoryController : ControllerBase
{
    private readonly IParsedHandRepository _repository;
    private readonly IHandHistoryMapper _mapper;

    public HandHistoryController(IParsedHandRepository repository, IHandHistoryMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<HandHistoryDto[]> GetHandHistories(CancellationToken cancellationToken)
    {
        var parsedHands = await _repository.GetParsedHands(cancellationToken);
        return parsedHands.Select(hand =>
        {
            var holeCardsDto = _mapper.Map(hand.HoleCards);
            return new HandHistoryDto(hand.HandId, holeCardsDto);
        }).ToArray();
    }
}