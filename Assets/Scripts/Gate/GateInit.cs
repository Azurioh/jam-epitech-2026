using UnityEngine;
using TMPro;

public class GateInit : MonoBehaviour, IDamageable
{
    public int health = 1000; 
    public int maxHealth = 1000;
    public enum TypeSelection { North, South, West, East }
    public TypeSelection direction;
    public GameObject gate;
    
    void Update()
    {
        if (!gate) return;
        if (health <= 0) {
            health = 0;
            gate.SetActive(false);
        } else {
            gate.SetActive(true);
        }
    }

    public void TakeDamage(float damage)
    {
        health -= (int)damage;
        if (health < 0)
            health = 0;
    }
}
