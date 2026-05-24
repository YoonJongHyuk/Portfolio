using System;
using UnityEngine;

[Serializable]
public class ItemTypeData
{
    public ItemType Type;
    public float effectAmount;
}

/// <summary>
/// 
/// </summary>
public enum ItemType
{
    Tiredness,
    Hunger,
    Physical,
    Illness,
    Stamina,
    Tongs
}