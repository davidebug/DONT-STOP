using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class Creator : MonoBehaviour
{
    // private int rayDistance = 1000;
    public Camera creatorCamera;

    private float spacing = 15f;
    
    // Start is called before the first frame update
    void Awake()
    {
        // controls = new PlayerControls();
    }

    private bool firstPlace = true;
    private void OnClickCreate()
    {
        spacing = PlaneHandler.instance.spacing;
        Vector3 spawnPosition;
        var ray = creatorCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        int layerMask = 1 << 8;
        //Debug.Log("OnClick called");
        if (Physics.Raycast(ray, out var hit, 100, layerMask) && !Pause.paused)
        {
            if (firstPlace)
            {
                TutorialController.istance.disableDialogBox("TutorialCreator");
                firstPlace = false;
            }

            //Debug.Log("Left click on " + hit.collider.gameObject.name);
            spawnPosition = hit.point;
            spawnPosition.z = ((int) ((spawnPosition.z + (spacing / 2)) / spacing)) * spacing;
            spawnPosition.y = ((int) ((spawnPosition.y + (spacing / 2)) / spacing)) * spacing;
            
            if (hit.point.x < 0)
                spawnPosition.x = ((int) ((spawnPosition.x - (spacing / 2)) / spacing)) * spacing;
            else
                spawnPosition.x = ((int) ((spawnPosition.x + (spacing / 2)) / spacing)) * spacing;
            
            if (!(PlaneHandler.instance.IsPlatformPresent(spawnPosition.x, spawnPosition.z, false)))
            {
                GameObject newPlatform;
                if (!(PlaneHandler.instance.tutorialPresent && spawnPosition.z == (PlaneHandler.instance.platformInTutorial-1)* PlaneHandler.instance.spacing))
                    newPlatform = PlatformSelectionUI.instance.PlaceAndChangeSelectedPlatform();
                else
                    newPlatform = PlatformSelectionUI.instance.PlaceSelectedPlatformAndPutTrampolineNext();

                if (newPlatform != null)
                {
                    PlaneHandler.instance.AddPlatform(new Vector3(spawnPosition.x, 0, spawnPosition.z),
                        newPlatform);

                    PlaneHandler.instance.AddEmptyTiles(spawnPosition);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}