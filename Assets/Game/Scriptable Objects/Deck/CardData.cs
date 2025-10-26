using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Scriptable Objects/CardData")]
public class CardData : ScriptableObject
{
    public string suit;
    public string rank;
    public int value;
    public Sprite sprite;
}
