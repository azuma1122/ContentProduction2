using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
///　JSON のデータ構造
/// </summary>
[Serializable]
public class ItemValue
{
    public int color;
    public int power;
    public int point;
}

[Serializable]
public class ItemEntry
{
    public string key;
    public ItemValue value;
}

[Serializable]
public class ItemList
{
    public List<ItemEntry> items;
}
