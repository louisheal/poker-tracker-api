using System.Text.Json.Serialization;

namespace PokerTrackerApi.Domain;

public enum Rank
{
    [JsonStringEnumMemberName("2")]
    Two = 2,
    [JsonStringEnumMemberName("3")]
    Three = 3,
    [JsonStringEnumMemberName("4")]
    Four = 4,
    [JsonStringEnumMemberName("5")]
    Five = 5,
    [JsonStringEnumMemberName("6")]
    Six = 6,
    [JsonStringEnumMemberName("7")]
    Seven = 7,
    [JsonStringEnumMemberName("8")]
    Eight = 8,
    [JsonStringEnumMemberName("9")]
    Nine = 9,
    [JsonStringEnumMemberName("T")]
    Ten = 10,
    [JsonStringEnumMemberName("J")]
    Jack = 11,
    [JsonStringEnumMemberName("Q")]
    Queen = 12,
    [JsonStringEnumMemberName("K")]
    King = 13,
    [JsonStringEnumMemberName("A")]
    Ace = 14,
}