using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSatatsSO", menuName = "Scriptable Objects/PlayerSatatsSO")]
public class PlayerSatatsSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Health")]
    public int maxHP = 5;

    [Header("Shooting")]
    public float fireRate = 4f;
    public float bulletSpeed = 12f;
    public float bulletRange = 8f;
    public int bulletDamage = 1;
    public int bulletCount = 1;
    public float bulletSize = 1f;
    public float spreadDeg = 0f;
    
}
