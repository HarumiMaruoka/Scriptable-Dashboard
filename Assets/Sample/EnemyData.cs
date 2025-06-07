using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyData : ScriptableObject
{
    [SerializeField, TextArea(0,9999)]
    private string _enemyName;
    [SerializeField]
    private int _health;
    [SerializeField]
    private float _speed;
    [SerializeField]
    private Color _color;
    [SerializeField]
    private Sprite _icon;
    [SerializeField]
    private GameObject _prefab;
}