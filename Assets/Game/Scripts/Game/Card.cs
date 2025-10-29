using UnityEngine;

public class Card
{
    public string suit;
    public int value;

    public Card(string suit, int value)
    {
        this.suit = suit;
        this.value = value;
    }

    public override bool Equals(object obj)
    {
        if (obj is Card other)
            return suit == other.suit && value == other.value;
        return false;
    }

    public override int GetHashCode()
    {
        return (suit, value).GetHashCode();
    }

    public static bool operator ==(Card a, Card b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Card a, Card b) => !(a == b);
}
