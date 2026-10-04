using Microsoft.AspNetCore.Mvc;
using PokerTrackerApi.HandNotes;

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
    public async Task<ActionResult<HandHistoryDto[]>> GetHandHistories(
        CancellationToken cancellationToken,
        bool? heroSawFlop = null,
        [FromQuery] string[]? labels = null,
        bool includeUnlabelled = false
    )
    {
        if (labels?.Any(label => !HandLabelCatalog.Contains(label)) == true)
        {
            return BadRequest("One or more labels are not recognized.");
        }

        var handHistories = await _repository.GetHandHistoriesAsync(
            cancellationToken,
            heroSawFlop,
            labels,
            includeUnlabelled
        );
        return Ok(
            handHistories
                .Select(hand =>
                {
                    var holeCardsDto = _mapper.Map(hand.HeroHoleCards);
                    return new HandHistoryDto(
                        hand.HandId,
                        holeCardsDto,
                        hand.Labels.Select(label => new HandLabelDto(
                                label.Street.ToString(),
                                label.Label
                            ))
                            .ToArray(),
                        hand.Note
                    );
                })
                .ToArray()
        );
    }
}
