using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RabbitController : MonoBehaviour
{
    public Animator m_Animator; 

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W)){
            m_Animator.SetBool("isRunning", true);
        }
        else
        {
            m_Animator.SetBool("isRunning", false);
        }

        if (Input.GetKey(KeyCode.Space))
        {
            m_Animator.SetBool("isJumping", true);
        }
        else
        {
            m_Animator.SetBool("isJumping", false);
        }

    }
}
