using System.Text.Json.Serialization;

namespace PokerTrackerApi.Domain;

public enum Suit
{
    [JsonStringEnumMemberName("d")]
    Diamonds,
    [JsonStringEnumMemberName("h")]
    Hearts,
    [JsonStringEnumMemberName("c")]
    Clubs,
    [JsonStringEnumMemberName("s")]
    Spades,
}