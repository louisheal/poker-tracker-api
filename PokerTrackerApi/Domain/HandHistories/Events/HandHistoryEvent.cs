using System.Text.Json.Serialization;

namespace PokerTrackerApi.Domain.HandHistories.Events;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "eventType")]
[JsonDerivedType(typeof(AntePostEvent), "ante-post")]
[JsonDerivedType(typeof(BigBlindPostEvent), "big-blind-post")]
[JsonDerivedType(typeof(CardsShownEvent), "cards-shown")]
[JsonDerivedType(typeof(CashDropEvent), "cash-drop")]
[JsonDerivedType(typeof(FlopDealtEvent), "flop-dealt")]
[JsonDerivedType(typeof(PlayerBetEvent), "player-bet")]
[JsonDerivedType(typeof(PlayerCallEvent), "player-call")]
[JsonDerivedType(typeof(PlayerCheckEvent), "player-check")]
[JsonDerivedType(typeof(PlayerFoldEvent), "player-fold")]
[JsonDerivedType(typeof(PlayerRaiseEvent), "player-raise")]
[JsonDerivedType(typeof(PotAwardedEvent), "pot-awarded")]
[JsonDerivedType(typeof(RiverDealtEvent), "river-dealt")]
[JsonDerivedType(typeof(SmallBlindPostEvent), "small-blind-post")]
[JsonDerivedType(typeof(TurnDealtEvent), "turn-dealt")]
[JsonDerivedType(typeof(UncalledBetReturnedEvent), "uncalled-bet-returned")]
[GenerateSerializer]
public abstract record HandHistoryEvent([property: Id(0)] int Sequence);
