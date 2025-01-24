using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum EHeardSoundCategory
{
    EFootstep,
    EJump
}

public class HearingManager : MonoBehaviour
{
    public static HearingManager Instance { get; private set; } = null;

    public List<HearingSensor> AllSensors { get; private set; } = new List<HearingSensor>();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Multiple HearingManagers found. Destroying " + gameObject.name);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    void Start()
    {

    }

    void Update()
    {

    }

    public void Register(HearingSensor sensor)
    {
        AllSensors.Add(sensor);
    }

    public void Deregister(HearingSensor sensor)
    {
        AllSensors.Remove(sensor);
    }

    public void OnSoundEmitted(GameObject source, Vector3 location, EHeardSoundCategory category, float intensity)
    {
        // Notify all sensors
        foreach(var sensor in AllSensors)
        {
            sensor.OnHeardSound(source, location, category, intensity);
        }
    }
}
