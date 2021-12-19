using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Animator _animator;
    bool shown = false;
    public float speed = 10;
    float oldspeed;
    Transform cameraTransform;
    public float lookSpeed = 3;
    private Vector2 rotation = Vector2.zero;
    Rigidbody _rigidbody;
    Transform body;
    public float castRange = 0.7f;
    bool Crawling = false;
    bool Hiding = false;
    Vector3 crawlToPoint = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        body = transform.GetChild(0);
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Look() // Look rotation (UP down is Camera) (Left right is Transform rotation)
    {
        rotation.y += Input.GetAxis("Mouse X");
        rotation.x += -Input.GetAxis("Mouse Y");
        rotation.x = Mathf.Clamp(rotation.x, -15f, 15f);
        transform.eulerAngles = new Vector2(0, rotation.y) * lookSpeed;
        cameraTransform = transform.Find("Camera");
        cameraTransform.localRotation = Quaternion.Euler(rotation.x * lookSpeed, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 movementVector = new Vector3(h,0,v);
        _animator.SetBool("Walking", movementVector != Vector3.zero);
        _animator.SetBool("Running", (movementVector != Vector3.zero && Input.GetKey(KeyCode.LeftShift)));
        if(Input.GetKeyDown(KeyCode.LeftShift))
        {
            oldspeed = speed;
            speed = oldspeed * 2;
        } else if(Input.GetKeyUp(KeyCode.LeftShift))
        {
            speed = oldspeed;
        }
        if(!Crawling)
            Look();
        if(!Hiding)
            transform.Translate(movementVector * speed * Time.deltaTime);
        if(Input.GetKeyDown(KeyCode.LeftControl))
        {
            shown ^= shown;
            Cursor.lockState = (!shown ? CursorLockMode.Locked : CursorLockMode.None );
        }
        RaycastHit hitInfo;
        if (Physics.Raycast(body.position, body.forward, out hitInfo, castRange))
        {
            if(hitInfo.transform.tag == "HidingSpot")
            {
                Debug.Log("Casting on hiding spot");
                Debug.DrawLine(transform.position, hitInfo.point, Color.green);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("Hiding inside " + hitInfo.transform.name);
                    _animator.SetBool("Hiding", true);
                    _rigidbody.isKinematic = true;
                    crawlToPoint = new Vector3(hitInfo.transform.position.x, transform.position.y, hitInfo.transform.position.z);
                    Crawling = true;
                }
            }

        }
        if(Crawling)
        {
            if (Vector3.Distance(transform.position, crawlToPoint) > 0.1)
                transform.Translate((crawlToPoint - transform.position) * 0.8f * Time.deltaTime);
            else
            {
                Crawling = false;
                Hiding = true;
            }
        }
    }

}
