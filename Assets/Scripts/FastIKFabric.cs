using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastIKFabric : MonoBehaviour
{
    public Transform target;
    public Transform pole;
    public int chainLength;//다리의 개수(뼈의 개수 - 1) ex) bone = 3 -> chainLength = 2

    protected float[] bonesLength; // 다리 하나하나의 길이
    protected float completeLength; // 다리의 전체 길이
    protected Transform[] bones; // 뼈
    protected Vector3[] positions; // 뼈들의 위치

    public int interations;
    float delta = 0.001f;

    protected Vector3[] startDirectionSucc;
    protected Quaternion[] startRotationBone;
    protected Quaternion startRotationTarget;
    protected Quaternion startRotationRoot;

    public Transform moveTarget;

    void Awake()
    {
        init();
    }

    private void init()
    {
        bones = new Transform[chainLength + 1];
        positions = new Vector3[chainLength + 1];
        bonesLength = new float[chainLength];

        startDirectionSucc = new Vector3[chainLength + 1];
        startRotationBone = new Quaternion[chainLength + 1];
        
        completeLength = 0;

        var curr = transform;

        for(int i = bones.Length - 1; i >= 0; i--)
        {
            bones[i] = curr;
            startRotationBone[i] = curr.rotation;
            if(i == bones.Length - 1)
            {
                //startDirectionSucc[i] = target.position - curr.position;
            }
            else
            {
                startDirectionSucc[i] = bones[i+1].position - curr.position;
                bonesLength[i] = (bones[i+1].position - curr.position).magnitude;
                completeLength += bonesLength[i];
            }
            curr = curr.parent.transform;
        }
    }

    void LateUpdate()
    {
        resolveIK();
    }

    private void resolveIK()
    {
        if(target == null) return;
        if(bonesLength.Length != chainLength) init();

        for(int i = 0; i < bones.Length; i++)
            positions[i] = bones[i].position;

        if((target.position - bones[0].position).sqrMagnitude >= completeLength*completeLength)
        {
            var direction = (target.position - positions[0]).normalized;

            for(int i = 1; i < positions.Length; i++)
            {
                positions[i] = positions[i-1] + direction*bonesLength[i-1];
            }
        }
        else
        {
            for(int iter = 0; iter < interations; iter++)
            {
                for(int i = positions.Length - 1; i > 0; i--)
                {
                    if(i == positions.Length - 1)
                        positions[i] = target.position;
                    else
                    {
                        positions[i] = positions[i+1] + (positions[i] - positions[i+1]).normalized * bonesLength[i];
                    }
                }

                for(int i = 1; i < positions.Length; i++)
                {
                    positions[i] = positions[i - 1] + (positions[i] - positions[i-1]).normalized * bonesLength[i-1];
                }

                if((positions[positions.Length - 1] - target.position).sqrMagnitude < delta * delta)
                    break;
            }
        }

        if(pole != null)
        {
            for(int i = 1; i < positions.Length - 1; i++)
            {
                var plane = new Plane(positions[i+1] - positions[i-1], positions[i-1]);
                var projPole = plane.ClosestPointOnPlane(pole.position);
                var projBone = plane.ClosestPointOnPlane(positions[i]);
                var angle = Vector3.SignedAngle(projBone - positions[i-1], projPole - positions[i-1], plane.normal);
                positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (positions[i] - positions[i-1]) + positions[i-1];
            }
        }

        for(int i = 0; i < positions.Length; i++)
        {
            if(i == positions.Length - 1)
            {
                bones[i].rotation = target.rotation;
            }
            else
            {
                bones[i].rotation = Quaternion.FromToRotation(startDirectionSucc[i], positions[i+1] - positions[i]);
            }

            bones[i].position = positions[i];
        }
    }
}
