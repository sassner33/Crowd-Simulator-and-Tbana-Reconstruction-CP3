using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using GLTFast.Addons;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Threading;
using UnityEngine.XR.Interaction.Toolkit;

public class ScenarioPicker : MonoBehaviour
{
    public Slider ExposureSlider;
    public GameObject CrowdAgents;
    public GameObject Pillars;
    public GameObject Walls;
    public GameObject GlassWalls;
    public GameObject Ads;
    public GameObject Benches;
    public GameObject Bins;
    public GameObject VendingMachines;
    public GameObject Trains;
    // NEW: actor spawn fields
    public Transform actorSpawnPoint;
    public GameObject ActorChildPrefab;
    public GameObject ActorAdultPrefab;
    public GameObject CaregiverPrefab;

    private bool[] scenariosPicked = new bool[16];
    System.Random random = new System.Random();
    private GameObject[] scenarioObjects;
    public GameObject RatingMenu;
    public GameObject FreeMenu;
    public CrowdToggle crowdToggle;
    public GameObject player;
    public Transform XRrig;
    public LocomotionSystem locomotionSystem;
    private ChangeExposure changeExposure;
    public Slider presetRatingSlider1;
    public Slider presetRatingSlider2;
    public Slider presetRatingSlider3;
    public int presetScenarioId;
    
    public TeleportCoordinates teleportCoordinates = new TeleportCoordinates
    {
        position = new Vector3(-42.745f, 0.628f, -2.7f),
        rotation = new Vector3(0f, 90f, 0f)
    };
    private TeleportCoordinates VRteleportCoordinates = new TeleportCoordinates
    {
        position = new Vector3(-40.53f, 0f, -3.465f),
        rotation = new Vector3(0f, 90f, 0f)
    };

    /*
    The preset scenarios are saved as follows:
    {ScenarioId, Lighting, CrowdDensity, Pillars, Walls, GlassWalls, Ads, Benches, Bins, VendingMachines, Trains, ActorType, Caregiver, Barrier}
    
    Lighting: 1=Bright, 2=Optima, 3=Dark
    CrowdDensity: 0=Low, 1=High
    Pillars, Walls, GlassWalls, Ads, Benches, Bins, VendingMachines, Trains: 1=On, 0=Off
    ActorType: 1=Child, 2=Adult
    Caregiver: 1=Present, 0=Absent
    Barrier: 1=On, 0=Off
    */
    public int[,] presetScenarios = new int[,]          
    {
        {1, 2, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},   //Scenario 1: Child, Low Crowd, Caregiver, Barrier ON
        {2, 2, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0},   //Scenario 2: Child, Low Crowd, Caregiver, Barrier OFF
        {3, 2, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1},   //Scenario 3: Child, Low Crowd, No Caregiver, Barrier ON
        {4, 2, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0},   //Scenario 4: Child, Low Crowd, No Caregiver, Barrier OFF
        {5, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},   //Scenario 5: Child, High Crowd, Caregiver, Barrier ON
        {6, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0},   //Scenario 6: Child, High Crowd, Caregiver, Barrier OFF
        {7, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1},   //Scenario 7: Child, High Crowd, No Caregiver, Barrier ON
        {8, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0},   //Scenario 8: Child, High Crowd, No Caregiver, Barrier OFF
        {9, 2, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1},   //Scenario 9: Adult, Low Crowd, Caregiver, Barrier ON
        {10, 2, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 0},  //Scenario 10: Adult, Low Crowd, Caregiver, Barrier OFF
        {11, 2, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 0, 1},  //Scenario 11: Adult, Low Crowd, No Caregiver, Barrier ON
        {12, 2, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 0, 0},  //Scenario 12: Adult, Low Crowd, No Caregiver, Barrier OFF
        {13, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1},  //Scenario 13: Adult, High Crowd, Caregiver, Barrier ON
        {14, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 0},  //Scenario 14: Adult, High Crowd, Caregiver, Barrier OFF
        {15, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 0, 1},  //Scenario 15: Adult, High Crowd, No Caregiver, Barrier ON
        {16, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 0, 0}   //Scenario 16: Adult, High Crowd, No Caregiver, Barrier OFF
    };


    void Start()
    {
        for (int i = 0; i < 16; i++)
        {
            scenariosPicked[i] = false;
        }
        scenarioObjects = new GameObject[]
        {
            Pillars, Walls, GlassWalls, Ads, Benches, Bins, VendingMachines, Trains
        };
        changeExposure = FindObjectOfType<ChangeExposure>();

        if (changeExposure == null)
        {
            Debug.LogError("ChangeExposure component not found in scene.");
        }
    }
    public bool pickRandScenario()
    {
        int[] falseIndices = scenariosPicked.Select((value, index) => new { value, index })
                                            .Where(x => !x.value)
                                            .Select(x => x.index)
                                            .ToArray();
        
        if(falseIndices.Length == 0)
        {
            Debug.Log("All Scenarios have been picked.");
            if (FreeMenu != null)
            {
                FreeMenu.SetActive(true);
            }
            return false;
        }
        int randomFalseIdx = falseIndices[random.Next(falseIndices.Length)];
        Debug.Log("Random False Index: " + randomFalseIdx);
        scenariosPicked[randomFalseIdx] = true;
        pickSpecificScenario(randomFalseIdx + 1);
        return true;
    }

    public void pickSpecificScenario(int scenarioId)
    {
        int scenarioIdx = scenarioId - 1;
        presetScenarioId = scenarioId;
        if (scenarioIdx < 0 || scenarioIdx >= scenariosPicked.Length)
        {
            Debug.LogError("Invalid scenario index " + scenarioIdx);
            return;
        }

        // Ensure any previous spawned clones are removed before applying the new scenario
        DespawnClones();

        int exposureSetting = presetScenarios[scenarioIdx, 1];

        if (changeExposure != null)
        {
            if (exposureSetting == 1)
            {
                Debug.Log("Exposure set by index to: 2");
                changeExposure.SetExposureByIndex(2);
            }
            if (exposureSetting == 2)
            {
                Debug.Log("Exposure set by index to: 1");
                changeExposure.SetExposureByIndex(1);

            }
            if(exposureSetting == 3)
            {
                Debug.Log("Exposure set by index to: 0");
                changeExposure.SetExposureByIndex(0);
            }
        }

        // NEW: deterministic crowd toggle (set desired state instead of blindly toggling)
        bool desiredCrowdOn = presetScenarios[scenarioIdx, 2] == 1;
        SetCrowdState(desiredCrowdOn);

        for (int i = 0; i < scenarioObjects.Length; i++)
        {
            int state = presetScenarios[scenarioIdx, i + 3]; //ignores the ScenarioId, the Lighting, and the Crowd
            ToggleObject(scenarioObjects[i], state);
        }

            // Handle ActorType (column 11)
            int actorType = presetScenarios[scenarioIdx, 11];
            // Handle Caregiver (column 12)
            int caregiverPresent = presetScenarios[scenarioIdx, 12];
            // Handle Barrier (column 13)
            int barrierState = presetScenarios[scenarioIdx, 13];

            Debug.Log($"ActorType: {actorType}  Caregiver: {caregiverPresent}  Barrier: {barrierState}");

            GameObject spawnedActor = HandleActorSpawning(actorType, caregiverPresent);

            // Safety: ensure spawned actor is active and not parented under CrowdAgents (which might get toggled off)
            if (spawnedActor != null)
            {
                if (!spawnedActor.activeInHierarchy)
                {
                    spawnedActor.SetActive(true);
                    Debug.Log("Spawned actor was inactive; reactivated.");
                }

                if (CrowdAgents != null && actorSpawnPoint != null && actorSpawnPoint.IsChildOf(CrowdAgents.transform))
                {
                    Debug.LogWarning("actorSpawnPoint is a child of CrowdAgents. Move the spawn point out of CrowdAgents to avoid actors being deactivated when crowd toggles. Unparenting spawned actor now.");
                    spawnedActor.transform.SetParent(null);
                }
            }

            HandleBarrier(barrierState);

            Debug.Log("Set Scenario Id: " + scenarioId);
    }
    
    public int GetScenarioId()
    {
        return presetScenarioId;
    }

    private void ToggleObject(GameObject obj, int state)
    {
        if (obj !=null)
        {
            obj.SetActive(state == 1);
        }
    }

    private void DespawnClones()
    {
        // destroy only the objects we spawn (tags are recommended)
        GameObject[] oldActors = GameObject.FindGameObjectsWithTag("Actor");
        for (int i = 0; i < oldActors.Length; i++)
        {
            if (oldActors[i] != null)
                Destroy(oldActors[i]);
        }

        GameObject[] oldCaregivers = GameObject.FindGameObjectsWithTag("Caregiver");
        for (int i = 0; i < oldCaregivers.Length; i++)
        {
            if (oldCaregivers[i] != null)
                Destroy(oldCaregivers[i]);
        }

        Debug.Log($"DespawnClones: removed {oldActors.Length} actor(s) and {oldCaregivers.Length} caregiver(s).");
    }

    private void SetCrowdState(bool on)
    {
        if (CrowdAgents == null) return;

        if (CrowdAgents.activeSelf == on) return;

        if (crowdToggle != null)
        {
            crowdToggle.ToggleElement();
            if (CrowdAgents.activeSelf != on)
            {
                CrowdAgents.SetActive(on);
                Debug.Log("SetCrowdState corrected active state directly.");
            }
        }
        else
        {
            CrowdAgents.SetActive(on);
        }

        Debug.Log("SetCrowdState: " + (on ? "High (ON)" : "Low (OFF)"));
    }


    private GameObject HandleActorSpawning(int actorType, int caregiverPresent)
    {
        Debug.Log("=== HandleActorSpawning called ===");

        // Clean up any existing clones (in case DespawnClones wasn't called)
        GameObject[] oldActors = GameObject.FindGameObjectsWithTag("Actor");
        foreach (GameObject a in oldActors) if (a != null) Destroy(a);

        GameObject[] oldCaregivers = GameObject.FindGameObjectsWithTag("Caregiver");
        foreach (GameObject c in oldCaregivers) if (c != null) Destroy(c);

        if (actorSpawnPoint == null)
        {
            Debug.LogError("Actor spawn point not assigned!");
            return null;
        }

        // Try resources fallback if inspector not set
        if (actorType == 1 && ActorChildPrefab == null)
        {
            ActorChildPrefab = Resources.Load<GameObject>("ActorChildPrefab");
            if (ActorChildPrefab != null) Debug.Log("Loaded ActorChildPrefab from Resources folder.");
        }
        if (actorType != 1 && ActorAdultPrefab == null)
        {
            ActorAdultPrefab = Resources.Load<GameObject>("ActorAdultPrefab");
            if (ActorAdultPrefab != null) Debug.Log("Loaded ActorAdultPrefab from Resources folder.");
        }

        GameObject actorPrefab = (actorType == 1) ? ActorChildPrefab : ActorAdultPrefab;
        if (actorPrefab == null)
        {
            Debug.LogError("Actor prefab not assigned for type: " + actorType + ".\nAssign the prefab in the ScenarioPicker inspector or place a prefab named 'ActorChildPrefab' or 'ActorAdultPrefab' in a Resources folder.");
            return null;
        }

        GameObject actor = Instantiate(actorPrefab, actorSpawnPoint.position, actorSpawnPoint.rotation);
        actor.transform.SetParent(this.transform, true);
        actor.SetActive(true);
        Debug.Log("Spawned actor type: " + (actorType == 1 ? "Child" : "Adult") + " at " + actorSpawnPoint.position);

        // NEW: Set actor to idle
        Agent actorAgent = actor.GetComponent<Agent>();
        if (actorAgent != null)
        {
            actorAgent.SetIdle(true);
            Debug.Log("Actor set to idle.");
        }

        // Caregiver (robust placement & ground alignment - fixed rotation and local offset)
        if (caregiverPresent == 1)
        {
            if (CaregiverPrefab == null)
            {
                CaregiverPrefab = Resources.Load<GameObject>("CaregiverPrefab");
                if (CaregiverPrefab != null) Debug.Log("Loaded CaregiverPrefab from Resources folder.");
            }

            if (CaregiverPrefab != null)
            {
                // horizontal offset to the side (right) so caregiver stands beside actor
                Vector3 right = actorSpawnPoint.right;
                right.y = 0f;
                if (right.sqrMagnitude < 0.001f) right = Vector3.right;
                right.Normalize();

                float sideDistance = 0.9f; // adjust to position caregiver to the side of actor
                Vector3 desired = actor.transform.position - right * sideDistance;

                // Keep caregiver at same vertical level as actor (avoid raycast hitting distant geometry)
                desired.y = actor.transform.position.y;

                // Set rotation to face same Y rotation as spawn (zero out pitch/roll to prevent upside-down)
                Quaternion rot = Quaternion.Euler(0f, actorSpawnPoint.eulerAngles.y, 0f);

                GameObject caregiver = Instantiate(CaregiverPrefab, desired, rot);
                caregiver.transform.SetParent(this.transform, true);
                caregiver.SetActive(true);

                // ensure it has correct tag so DespawnClones can remove it later
                try { caregiver.tag = "Caregiver"; } catch { /* ignore if tag doesn't exist */ }

                // NEW: Set caregiver to idle
                Agent caregiverAgent = caregiver.GetComponent<Agent>();
                if (caregiverAgent != null)
                {
                    caregiverAgent.SetIdle(true);
                    Debug.Log("Caregiver set to idle.");
                }

                // if prefab includes Animator with root motion, disable applyRootMotion so it doesn't drift unexpectedly
                Animator anim = caregiver.GetComponentInChildren<Animator>();
                if (anim != null && anim.applyRootMotion)
                {
                    anim.applyRootMotion = false;
                    Debug.Log("Disabled Animator.applyRootMotion on caregiver to prevent drift.");
                }

                Debug.Log("Spawned caregiver at " + desired + " rotationY=" + actorSpawnPoint.eulerAngles.y);
            }
            else
            {
                Debug.LogWarning("Caregiver prefab not assigned and not found in Resources; skipping caregiver spawn.");
            }
        }

        return actor;
    }


    private void HandleBarrier(int barrierState)
    {
        if (GlassWalls != null)
        {
            GlassWalls.SetActive(barrierState == 1);
            Debug.Log("Barrier: " + (barrierState == 1 ? "ON" : "OFF"));
        }
    }

    public void startScenario()
    {
        if (FreeMenu != null)
        {
            FreeMenu.SetActive(false);
        }
        if(!pickRandScenario())
        {
            pickSpecificScenario(1);    //For when we start the Freeplay
            return;
        }
        StartCoroutine(TimerCoroutine());
        
    }

    public void TeleportPlayer()
    {
        if (player != null && teleportCoordinates != null)
        {
            player.transform.position = teleportCoordinates.position;
            player.transform.rotation = Quaternion.Euler(teleportCoordinates.rotation);
            
            LookControl lookControl = FindObjectOfType<LookControl>();
            if (lookControl != null)
            {
                lookControl.SetInitialRotation();
            }
        }
        StartCoroutine(TeleportVRCoroutine());
    }

    private IEnumerator TeleportVRCoroutine()
    {
        if (XRrig != null && VRteleportCoordinates != null)
        {
            if (locomotionSystem != null)
            {
                locomotionSystem.enabled = false;
                Debug.Log("Locomotion disabled");
            }

            yield return new WaitForEndOfFrame();

            XRrig.transform.position = VRteleportCoordinates.position;
            XRrig.transform.rotation = Quaternion.Euler(VRteleportCoordinates.rotation);
            Debug.Log("updated VR coordinates: " + VRteleportCoordinates.position);
            
            yield return new WaitForEndOfFrame();

            if (locomotionSystem != null)
            {
                locomotionSystem.enabled = true;
            }
        }
    }



    private IEnumerator TimerCoroutine()
    {
        yield return new WaitForSeconds(12);
        if (RatingMenu != null)
        {
            RatingMenu.SetActive(true);
            presetRatingSlider1.value = 0;
            presetRatingSlider2.value = 0;
            presetRatingSlider3.value = 0;
            GameManager.Instance.SetMovementPause(true);
        }
    }
}