public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}

public enum Rank
{
    Two, Three, Four, Five, Six,
    Seven, Eight, Nine, Ten,
    Jack, Queen, King, Ace
}

public class Card
{
    public Suit Suit { get; private set; }
    public Rank Rank { get; private set; }

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