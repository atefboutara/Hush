using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EquipItem
{
    public GameObject itemPrefab;
    public Transform itemParent;
}

public class EquipItems : MonoBehaviour
{
    [SerializeField]
    public EquipItem[] items;
    // Start is called before the first frame update
    /*void Start()
    {

    }*/

    public void EquipAll()
    {
        foreach (EquipItem ei in items)
        {
            /*GameObject temp = */
            Instantiate(ei.itemPrefab, ei.itemParent);
        }
    }
}
