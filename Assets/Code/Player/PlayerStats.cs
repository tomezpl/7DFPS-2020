using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int health = 100;
    public PlayerScore score;
    public PlayerStats lastAttacker;

    // Start is called before the first frame update
    void Start()
    {
        score = new PlayerScore();
    }

    // Update is called once per frame
    void Update()
    {
        if(health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (lastAttacker != null)
        {
            lastAttacker.score.Kills++;
        }
        Destroy(gameObject);
    }
}
