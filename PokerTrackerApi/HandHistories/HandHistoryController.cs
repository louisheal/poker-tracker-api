using Microsoft.AspNetCore.Mvc;
using PokerTrackerApi.HandAnnotations;

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
        bool includeUnlabelled = false,
        bool flaggedOnly = false
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
            includeUnlabelled,
            flaggedOnly
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
                        hand.Note,
                        hand.Flagged
                    );
                })
                .ToArray()
        );
    }

    [HttpPost("by-ids")]
    public async Task<ActionResult<HandHistoryDto[]>> GetHandHistoriesByIds(
        [FromBody] string[] handIds,
        CancellationToken cancellationToken
    )
    {
        var handHistories = await _repository.GetHandHistoriesByIdsAsync(
            handIds,
            cancellationToken
        );
        return Ok(
            handHistories
                .Select(hand => new HandHistoryDto(
                    hand.HandId,
                    _mapper.Map(hand.HeroHoleCards),
                    hand.Labels.Select(label => new HandLabelDto(
                            label.Street.ToString(),
                            label.Label
                        ))
                        .ToArray(),
                    hand.Note,
                    hand.Flagged
                ))
                .ToArray()
        );
    }
}
