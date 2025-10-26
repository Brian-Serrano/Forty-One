using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ConfigHandler", menuName = "Scriptable Objects/ConfigHandler")]
public class ConfigHandler : ScriptableObject
{
    public List<CardData> deck;
}
