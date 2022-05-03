using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Assigns a random colour with 66%-75% intensity at <see cref="Start"/>.
/// </summary>
public class AssignRandomAlbedo : MonoBehaviour
{
    public string ColorPropertyName;

    // Start is called before the first frame update
    void Start()
    {
        if (string.IsNullOrWhiteSpace(ColorPropertyName))
        {
            GetComponent<Renderer>().material.color = Random.ColorHSV(0, 1f, 0, 0.75f, 0.66f, 1f);
        }
        else
        {
            GetComponent<Renderer>().material.SetColor(ColorPropertyName, Random.ColorHSV(0, 1f, 0, 0.75f, 0.66f, 1f));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
