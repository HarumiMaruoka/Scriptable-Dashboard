using System;
using UnityEngine;

public class EnemyData : ScriptableObject
{
    [SerializeField, TextArea]
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
