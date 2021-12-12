using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Animator _animator;
    bool shown = false;
    public float speed = 10;
    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    public float lookSpeed = 3;
    private Vector2 rotation = Vector2.zero;
    public void Look() // Look rotation (UP down is Camera) (Left right is Transform rotation)
    {
        rotation.y += Input.GetAxis("Mouse X");
        rotation.x += -Input.GetAxis("Mouse Y");
        rotation.x = Mathf.Clamp(rotation.x, -15f, 15f);
        transform.eulerAngles = new Vector2(0, rotation.y) * lookSpeed;
        transform.Find("CM vcam1").localRotation = Quaternion.Euler(rotation.x * lookSpeed, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        Look();
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 movementVector = new Vector3(h,0,v);

        _animator.SetBool("Walking", movementVector != Vector3.zero);
        _animator.SetBool("Running", (movementVector != Vector3.zero && Input.GetKeyDown(KeyCode.LeftShift)));

        transform.Translate(movementVector * speed * Time.deltaTime);
        if(Input.GetKeyDown(KeyCode.LeftControl))
        {
            shown ^= shown;
            Cursor.lockState = (!shown ? CursorLockMode.Locked : CursorLockMode.None );
        }
    }
}
