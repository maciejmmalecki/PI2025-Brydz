using System;
/// <summary>
/// Reprezentuje pojedynczą licytację złożoną z poziomu i koloru
/// </summary>

[Serializable]
public class BiddingData
{
    public string level;
    public string suit;
    
    /// <summary>
    /// Zwraca tekstową reprezentację licytacji
    /// </summary>
    public override string ToString()
    {
        return string.IsNullOrEmpty(level) || string.IsNullOrEmpty(suit) ? "Pas" : level + suit;
    }

    /// <summary>
    /// Tworzy obiekt reprezentujący pas
    /// </summary>
    public static BiddingData Pass()
    {
        return new BiddingData { level = "", suit = "" };
    }

    /// <summary>
    /// Tworzy obiekt BiddingData z ciągu znaków
    /// </summary>
    /// <param name="bid">Tekst licytacji</param>
    /// <returns>Obiekt BiddingData</returns>
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