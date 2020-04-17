﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class EventRef_Follow : MonoBehaviour
{
    [EventRef]
    public string event_Ref;
    private EventInstance event_Instance;
    private EventDescription event_Description;
    [HideInInspector]
    public PARAMETER_ID isFollowParameterId;
    [HideInInspector]
    public float maxDistance;
    private float minDistance;
    private bool is3D;

    [SerializeField]
    private bool playFromStart = default;
    [SerializeField]
    private bool debug = default;


    private void Start()
    {
        Init_Ref();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawIcon(transform.position, "FMOD/FMODEmitter.tiff", true);
        if (debug)
            Gizmos.DrawWireSphere(transform.position, maxDistance);
    }

    public void Init_Ref()
    {
        event_Instance = RuntimeManager.CreateInstance(event_Ref);
        event_Description = RuntimeManager.GetEventDescription(event_Ref);
        event_Description.getMaximumDistance(out maxDistance);
        event_Description.getMinimumDistance(out minDistance);

        EventDescription isFollowEventDescription;
        event_Instance.getDescription(out isFollowEventDescription);
        PARAMETER_DESCRIPTION isFollowParameterDescription;
        isFollowEventDescription.getParameterDescriptionByName("isFollow", out isFollowParameterDescription);
        isFollowParameterId = isFollowParameterDescription.id;

        event_Description.is3D(out is3D);
        if (is3D)
            Init_Attachment(transform, GetComponent<Rigidbody>());
        if (playFromStart)
            Init_Event();
    }

    public void Init_Attachment(Transform position, Rigidbody rb)
    {
        RuntimeManager.AttachInstanceToGameObject(event_Instance, position, rb);
    }

    public void Init_Event()
    {
        event_Instance.start();
    }

    public void Init_Cue()
    {
        event_Instance.triggerCue();
    }

    public void Set_Parameter(PARAMETER_ID id, float value)
    {
        event_Instance.setParameterByID(id, value);
    }
}
