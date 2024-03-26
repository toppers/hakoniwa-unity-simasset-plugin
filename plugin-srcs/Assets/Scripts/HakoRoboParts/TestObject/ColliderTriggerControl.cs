using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderTriggerControl : MonoBehaviour
{
    public Collider obj;
    // Start is called before the first frame update
    void Start()
    {
        if (obj == null)
        {
            throw new System.Exception("Can not find Collider");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            obj.isTrigger = true;
            Debug.Log("key down A: " + obj.isTrigger);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            obj.isTrigger = false;
            Debug.Log("key down D: " + obj.isTrigger);
        }
    }
}
