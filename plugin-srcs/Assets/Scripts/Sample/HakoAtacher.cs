using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HakoAtacher : MonoBehaviour
{
    [SerializeField]
    private string class_name;

    private IAddAttach obj = null;
    // Start is called before the first frame update
    void Start()
    {
        var componentType = Type.GetType(this.class_name);
        if (componentType == null)
        {
            Debug.Log("Failed to find script: " + this.class_name);
            return;
        }
        this.gameObject.AddComponent(componentType);
        this.obj = this.gameObject.GetComponentInChildren<IAddAttach>();
        if (this.obj == null)
        {
            Debug.Log("Failed to find IAddAttach");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (this.obj == null)
        {
            return;
        }
        this.obj.Hello();
    }
}
