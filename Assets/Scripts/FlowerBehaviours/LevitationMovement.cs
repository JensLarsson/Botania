﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Timers;

public class LevitationMovement : MonoBehaviour
{
    [SerializeField] Vector3 _movementDirection;
    [SerializeField, Range(0, 1.57f)] float _randomDirectionOffset;
    [SerializeField] MeshRenderer _renderer;
    [SerializeField] Transform _childTran;
    [SerializeField] float _heightChangeSpeed = 1;
    [SerializeField] float _heighToGroundDifference = 20;
    [SerializeField] LayerMask _mask;
    Velocity _velocity;
    Vector3 _position;
    private System.Timers.Timer _timer;
    private void Awake()
    {
        if (_childTran == null)
        {
            _childTran = transform.GetChild(0);
        }
        _position = transform.localPosition;
        AddRandomDirectionOffset();
        if (_renderer != null)
        {
            _renderer.material.SetFloat("_Random", Random.Range(0.0f, 6.28f));
        }
        _velocity = new Velocity { transform = this.transform, direction = _movementDirection };

        _timer = new System.Timers.Timer();
        _timer.Interval = 20000;
        _timer.Elapsed += _timer_Elapsed;
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }
    private void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        AdjustHeight();
    }





    private void OnEnable()
    {
        try
        {
            BatchMovement.Instance.Subscribe(_velocity);
        }
        catch
        {
            Debug.LogError("BatchMovement.cs not found on scene, simply add that script to any singleton purpuse object");
        }
    }
    private void OnDisable()
    {
        BatchMovement.Instance.UnSubscribe(_velocity);
        _timer.Dispose();
    }





    void AddRandomDirectionOffset()
    {
        float sincosVal = Random.Range(-_randomDirectionOffset, _randomDirectionOffset);
        float sin = Mathf.Sin(sincosVal);
        float cos = Mathf.Cos(sincosVal);
        float x = _movementDirection.x * cos - _movementDirection.y * sin;
        float z = _movementDirection.x * sin + _movementDirection.y * cos;
        _movementDirection.x = x;
        _movementDirection.z = z;
    }





    void AdjustHeight()
    {
        RaycastHit hit;
        if (Physics.Raycast(_childTran.position, -Vector3.up, out hit, _mask))
        {
            Debug.Log("Changing Height");
            float targetHeight = hit.point.y + _heighToGroundDifference;
            StartCoroutine(LerpToHeight(_heightChangeSpeed, targetHeight));
        }
    }
    IEnumerator LerpToHeight(float lerpTime, float targetHeight)
    {
        float startHeight = _childTran.position.y;
        float time = 0;
        while (time < lerpTime)
        {
            Vector3 currentPos = _childTran.position;
            currentPos.y = Mathf.Lerp(startHeight, targetHeight, time / lerpTime);
            _childTran.position = currentPos;
            time += Time.deltaTime;
            yield return null;
        }
    }
}
