using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform fenggePet;
    [SerializeField] private Transform shaolanPet;
    [SerializeField] private Transform sungePet;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            fenggePet.GetComponent<UIPulseScale>().PlayPulse(5);
            shaolanPet.GetComponent<UIPulseScale>().PlayPulse(5);
            sungePet.GetComponent<UISwing>().PlaySwing(5f);
        }
    }
}
