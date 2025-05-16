using System;

[Serializable]
public class BiddingData
{
    public string level;
    public string suit;

    public override string ToString()
    {
        return string.IsNullOrEmpty(level) || string.IsNullOrEmpty(suit) ? "Pas" : level + suit;
    }

    public static BiddingData Pass()
    {
        return new BiddingData { level = "", suit = "" };
    }

    public static BiddingData FromString(string bid)
    {
        if (string.IsNullOrEmpty(bid) || bid == "Pas")
        {
            return Pass();
        }

        string level = bid.Substring(0, 1);
        string suit = bid.Substring(1).ToUpper();

        return new BiddingData { level = level, suit = suit };
    }
}