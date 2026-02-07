using UnityEngine;
using TMPro;

public class GateInit : MonoBehaviour
{
    public int health = 1000; 
    public int maxHealth = 1000;
    public enum TypeSelection { North, South, West, East }
    public TypeSelection direction;
}
