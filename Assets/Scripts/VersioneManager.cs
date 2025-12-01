using TMPro;
using UnityEngine;

public class VersioneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<TMP_Text>().text = Application.productName+" (v. "+Application.version+")";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
