using CustomVolumeComponent;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera)), DisallowMultipleComponent]
public class Camera1999 : MonoBehaviour
{
    public LayerMask FocusLayer;
    [Header("Sway")]
    [Range(0, 1)]
    public float SwayStrength = .2f;
    [Range(0, 1)]
    public float SwaySpeed = .1f;
    [Header("Lerp")]
    [Range(0, 1)]
    public float PositionLerp = .05f;
    public float FocusDistance = 4.5f;
    public float FocusPositionSafeRange = 1.5f;
    [Range(0, 1)]
    public float RotationLerp = .03f;
    public float FocusRotationSafeRange = 1;
    public Vector3 LookatAdjust = Vector3.up * .5f;
    [Header("Renderer Trick")]
    public Volume volume;
    public float FocusDepthBlurLerp = .1f;
    public float FocusDepthBlurStart = 50;
    public float FocusDepthBlurEnd = 23;

    Transform trans;
    Camera cam;
    Vector3 originPos;
    Quaternion originRot;
    float focusDepthBlurRate;

    Transform focusTrans;

    private void Start()
    {
        trans = transform;
        cam = GetComponent<Camera>();
        originPos = trans.position;
        originRot = trans.rotation;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, cam.farClipPlane, FocusLayer))
                focusTrans = hit.transform;
            else
                focusTrans = null;
        }

        Vector3 pos = originPos;
        Quaternion rot = originRot;
        
        if (focusTrans != null)
        {
            Vector3 focusPos = focusTrans.position + LookatAdjust;
            Vector3 focusDir = Vector3.Normalize(focusPos - originPos);
            pos = focusPos - focusDir * FocusDistance;
            pos.x = Mathf.Clamp(focusPos.x, originPos.x - FocusPositionSafeRange, originPos.x + FocusPositionSafeRange);
        }
        Vector3 noise = new Vector3((Mathf.PerlinNoise(Time.time * SwaySpeed, 0) - .5f) * SwayStrength, 0, 0);
        trans.position = Vector3.Lerp(trans.position, pos + noise, PositionLerp);

        if (focusTrans != null)
        {
            Vector3 focusPos = focusTrans.position + LookatAdjust;
            focusPos.x = Mathf.Clamp(focusPos.x, originPos.x - FocusRotationSafeRange, originPos.x + FocusRotationSafeRange);
            rot = Quaternion.LookRotation(Vector3.Normalize(focusPos - trans.position), Vector3.up);
        }
        trans.rotation = Quaternion.Lerp(trans.rotation, rot, RotationLerp);

        focusDepthBlurRate = Mathf.Lerp(focusDepthBlurRate, focusTrans == null ? 0 : 1, FocusDepthBlurLerp);
        InjectDepthFocus();
    }

    void InjectDepthFocus()
    {
        if (volume == null || !volume.profile.TryGet(out DepthBlur depthBlur))
            return;
        float res = CalcDepthFocus();
        depthBlur.DepthSetting.DepthFocus.Override(res);
    }

    float CalcDepthFocus()
    {
        float res = FocusDepthBlurStart + (FocusDepthBlurEnd - FocusDepthBlurStart) * focusDepthBlurRate;
        return res;
    }
}