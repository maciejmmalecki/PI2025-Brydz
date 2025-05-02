public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades,
    NT
}

public enum Rank
{
    Two, Three, Four, Five, Six,
    Seven, Eight, Nine, Ten,
    Jack, Queen, King, Ace
}

/// <summary>
/// Reprezentuje pojedynczą kartę w talii.
/// </summary>

public class Card
{
    public Suit Suit { get; private set; }
    public Rank Rank { get; private set; }

    /// <summary>
    /// Tworzy nową kartę o podanym kolorze i wartości.
    /// </summary>

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public override string ToString()
    {
        return $"{Rank} of {Suit}";
    }
}