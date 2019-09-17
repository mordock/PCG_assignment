using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCol : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter2D(Collision2D col) {
        Debug.Log("BOB2");
        if (col.gameObject.tag.Equals("Wall"))
        {
            Destroy(col.gameObject);
            Debug.Log("BOB");
        }
    }
}
