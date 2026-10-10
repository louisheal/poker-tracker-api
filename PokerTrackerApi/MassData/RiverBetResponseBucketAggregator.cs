namespace PokerTrackerApi.MassData;

internal record PostflopBetResponseObservation(
    PostflopBetResponseLine Line,
    PostflopResponseAction Action,
    decimal? BetToPotRatio
);

internal static class PostflopBetResponseBucketAggregator
{
    private static readonly int[] Thresholds = Enumerable
        .Range(1, 30)
        .Select(value => value * 10)
        .ToArray();

    internal static IReadOnlyList<PostflopBetResponseBucketDto> Aggregate(
        IEnumerable<PostflopBetResponseObservation> observations
    )
    {
        var countsByLine = Enum.GetValues<PostflopBetResponseLine>()
            .ToDictionary(
                line => line,
                _ => (Opportunities: new int[Thresholds.Length], Folds: new int[Thresholds.Length])
            );

        foreach (var observation in observations)
        {
            if (!observation.BetToPotRatio.HasValue || observation.BetToPotRatio.Value > 3m)
            {
                continue;
            }

            var counts = countsByLine[observation.Line];
            for (var thresholdIndex = 0; thresholdIndex < Thresholds.Length; thresholdIndex++)
            {
                if (observation.BetToPotRatio.Value > Thresholds[thresholdIndex] / 100m)
                {
                    continue;
                }

                counts.Opportunities[thresholdIndex]++;
                if (observation.Action == PostflopResponseAction.Fold)
                {
                    counts.Folds[thresholdIndex]++;
                }
            }
        }

        return Enum.GetValues<PostflopBetResponseLine>()
            .SelectMany(line =>
                Thresholds.Select(
                    (threshold, index) =>
                    {
                        var counts = countsByLine[line];
                        return new PostflopBetResponseBucketDto(
                            line.ToString(),
                            threshold,
                            counts.Opportunities[index],
                            counts.Folds[index]
                        );
                    }
                )
            )
            .ToArray();
    }
}
