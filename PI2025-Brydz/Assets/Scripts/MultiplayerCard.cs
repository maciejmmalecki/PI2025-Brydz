using System;

public class MultiplayerCard
{
    public string suit;
    public string rank;

    public MultiplayerCard(string suit, string rank)
    {
        this.suit = suit;
        this.rank = rank;
    }

    public override string ToString()
    {
        return suit + rank;
    }
}