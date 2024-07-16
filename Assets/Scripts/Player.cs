using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class Player : MonoBehaviour
{
    public static Player Instance;

    public float walkingSpeed = 7.5f;
    public Camera playerCamera;
    public float lookSpeed = 2.0f;

    public AudioSource footstepSound;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    [HideInInspector]
    public bool canMove;

    private List<GameObject> inRange = new List<GameObject>();
    private GameObject selected;
    private GameObject carrying;
    private bool walking = false;

    // DEBUG variable
    private bool DEBUG = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        canMove = false;
        // Lock cursor
        ToggleMouse();
    }

    void Update()
    {
        // Gate condition (not update if player can't move
        if (!canMove)
        {
            footstepSound.Stop();
            return;
        }

        // We are grounded, so recalculate move direction based on axes
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float curSpeedX = canMove ? walkingSpeed * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? walkingSpeed * Input.GetAxis("Horizontal") : 0;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -10.0f, 40.0f);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);

        // Check for movement
        bool movement = (curSpeedX + curSpeedY) != 0;

        // Starting to walk
        if (!walking && movement)
        {
            walking = true;

            // Start walking sound
            footstepSound.Play();
        }

        // Stopping to walk
        if (walking && !movement)
        {
            walking = false;

            // Start walking sound
            footstepSound.Stop();
        }

        // Crate Selection
        if (Input.GetKeyDown("tab"))
        {
            if (inRange.Count > 1)
            {
                // Save selected in TEMP
                GameObject temp = selected;
                inRange.Remove(temp);

                // Swap selected Crate
                UnselectCrate();
                SelectCrate(inRange[0]);

                // Re-add Crate to the inRange qeueu
                inRange.Add(temp);
            }
        }

        // Crate pickup if in range (ie, some crate is selected)
        if ((selected != null) && (Input.GetKeyDown("e") || Input.GetKeyDown("return")))
            PickupSelected();
    }
    

    public void ToggleMouse()
    {
        // Cursor Handling
        Cursor.visible = !canMove;

        if (!canMove)
            Cursor.lockState = CursorLockMode.None;

        if (canMove)
            Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Deposit Zone
        if ((other.name == "ToolDeposit") && (carrying != null))
        {
            string toolName = carrying.name.Replace("Crate-", "");
            if (DEBUG)
                Debug.Log("Dropping item " + toolName);
            Game.Instance.Deliver(toolName);

            // Destroying delivered tool
            Destroy(carrying);
            carrying = null;
        }

        // Crate Zone
        if (!(carrying != null) && other.name.StartsWith("Crate-"))
        {
            if (DEBUG)
                Debug.Log("Interacting with: " + other.name);

            // Adding to InRange objects
            inRange.Add(other.gameObject);

            // Set Selected to the collided object if nothing is already selected
            if(selected == null)
            {
                SelectCrate(other.gameObject);
            }
            else
            {
                Game.Instance.focusSwitchInfo.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Crate 
        if (!(carrying != null) && other.name.StartsWith("Crate-"))
        {
            if (DEBUG)
                Debug.Log("Stopping to interact with: " + other.name);

            // Remove from the InRange objects
            inRange.Remove(other.gameObject);

            // Update the Selected variable
            if (selected == other.gameObject)
            {
                UnselectCrate();

                if (inRange.Count > 0)
                    SelectCrate(inRange[0]);
            }

            if (inRange.Count <= 1)
                Game.Instance.focusSwitchInfo.SetActive(false);
        }
    }

    private void UnselectCrate()
    {
        // Unset input info
        Game.Instance.carryInfo.SetActive(false);

        // Reset material color to default
        Renderer crateRenderer = selected.GetComponent<Renderer>();
        crateRenderer.material.SetColor("_Color", new Color(1, 1, 1, 1)); // White

        // Set Selected variable
        selected = null;
    }

    private void SelectCrate(GameObject crate)
    {
        // Set input info
        Game.Instance.carryInfo.SetActive(true);

        // Set material color to light green
        Renderer crateRenderer = crate.GetComponent<Renderer>();
        crateRenderer.material.SetColor("_Color", new Color(0.387f, 1, 0.333f, 1)); // Green

        // Set Selected variable
        selected = crate;
    }

    private void PickupSelected()
    {
        // Set selected as carrying
        carrying = selected;
        UnselectCrate();
        inRange.Clear();

        // Setting crate as child of Player
        carrying.transform.SetParent(gameObject.transform);
        carrying.transform.localPosition = new Vector3(0.0f, 1.0f, 0.6f);
        carrying.transform.localRotation = Quaternion.Euler(0, 180, 0);
    }

}