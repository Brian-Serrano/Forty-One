using UnityEngine;

public class CardInfo : MonoBehaviour
{
    public string suit;
    public string rank;
    public int value;
    public Sprite sprite;
    public bool isFaceUp = false;

    public void Initialize(string suit, int value, Sprite sprite, string rank)
    {
        this.suit = suit;
        this.value = value;
        this.sprite = sprite;
        this.rank = rank;
    }
}
