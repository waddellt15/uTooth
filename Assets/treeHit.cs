using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class treeHit : MonoBehaviour
{
    // Start is called before the first frame update
    public BoxCollider treeBox;
    public string hit;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        hit = "1";
    }
    private void OnTriggerExit(Collider other)
    {
        hit = "0";
    }
}
