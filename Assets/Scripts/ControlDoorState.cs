using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlDoorState : MonoBehaviour
{
    Animator anim;
    bool closed = true;
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void ToggleDoor()
    {
        closed = !closed;
        anim.SetTrigger("Interact");
        anim.SetBool("Closed", closed);
    }
}
