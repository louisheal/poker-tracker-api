using Microsoft.AspNetCore.Mvc;
using PokerTrackerApi.Domain;
using PokerTrackerApi.HandHistories;

namespace PokerTrackerApi.HandAnnotations;

[ApiController]
[Route("api/handannotations")]
public class HandAnnotationsController : ControllerBase
{
    private readonly IHandAnnotationsRepository _repository;

    public HandAnnotationsController(IHandAnnotationsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("labels")]
    public ActionResult<IReadOnlyList<HandLabelOption>> GetLabels() => Ok(HandLabelCatalog.All);

    [HttpPut("{handId}/note")]
    public async Task<ActionResult<string>> ReplaceNote(
        string handId,
        UpdateHandNoteRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!await _repository.HandExistsAsync(handId, cancellationToken))
        {
            return NotFound();
        }

        await _repository.ReplaceNoteAsync(handId, request.Note, cancellationToken);
        return Ok(request.Note);
    }

    [HttpPut("{handId}/flagged")]
    public async Task<ActionResult<bool>> SetFlagged(
        string handId,
        UpdateHandFlaggedRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!await _repository.HandExistsAsync(handId, cancellationToken))
        {
            return NotFound();
        }

        await _repository.SetFlaggedAsync(handId, request.Flagged, cancellationToken);
        return Ok(request.Flagged);
    }

    [HttpPut("{handId}/labels")]
    public async Task<ActionResult<HandLabelDto[]>> ReplaceLabels(
        string handId,
        UpdateHandLabelsRequest request,
        CancellationToken cancellationToken
    )
    {
        var assignments = new List<HandLabelAssignment>();
        foreach (var (streetName, streetLabels) in request.LabelsByStreet)
        {
            if (!LabelStreets.TryGetValue(streetName, out var street))
            {
                return BadRequest("One or more streets are not recognized.");
            }

            var distinctLabels = streetLabels.Distinct().ToArray();
            if (distinctLabels.Any(label => !HandLabelCatalog.Contains(label)))
            {
                return BadRequest("One or more labels are not recognized.");
            }

            assignments.AddRange(
                distinctLabels.Select(label => new HandLabelAssignment
                {
                    HandId = handId,
                    Street = street,
                    Label = label,
                })
            );
        }

        if (!await _repository.HandExistsAsync(handId, cancellationToken))
        {
            return NotFound();
        }

        await _repository.ReplaceLabelsAsync(handId, assignments, cancellationToken);
        return Ok(
            assignments.Select(assignment => new HandLabelDto(
                assignment.Street.ToString(),
                assignment.Label
            ))
        );
    }

    private static readonly Dictionary<string, PokerStreet> LabelStreets = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        [nameof(PokerStreet.Flop)] = PokerStreet.Flop,
        [nameof(PokerStreet.Turn)] = PokerStreet.Turn,
        [nameof(PokerStreet.River)] = PokerStreet.River,
    };
}

public record UpdateHandLabelsRequest(Dictionary<string, string[]> LabelsByStreet);

public record UpdateHandNoteRequest(string Note);

public record UpdateHandFlaggedRequest(bool Flagged);
