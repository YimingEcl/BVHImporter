using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BVHAnimation : MonoBehaviour
{
    public string fileName;
    public string bvhPrefix;
    //public Animator myAnimator;
    private AnimationClip myClip;
    public Remap[] remap = null;

    private Dictionary<string, string> nameMap;
    private Transform rootTransform;
    private Transform targetTransform;
    private int frames;
    private int frameRate;
    private Animation anim;
    private string path;
    Keyframe[][] keyframes;

    public BVHImporter bi;

    [Serializable]
    public struct Remap
    {
        public string bvhName;
        public string targetName;
    }

    public void Read()
    {
        bi = new BVHImporter(fileName);
        bi.bvhPrefix = this.bvhPrefix;
        bi.read();
        this.frames = bi.frames;
        this.frameRate = bi.frameRate;
        Debug.Log("Read Successfuly!");
    }

    public void Start()
    {
        bi = new BVHImporter(fileName);
        bi.bvhPrefix = this.bvhPrefix;
        bi.read();
        this.frames = bi.frames;
        this.frameRate = bi.frameRate;
        nameMap = new Dictionary<string, string>();

        for (int i = 0; i < remap.Length; i++)
        {
            if(remap[i].targetName != string.Empty)
            {
                nameMap.Add(remap[i].bvhName, remap[i].targetName);
            }
        }

        //myAnimator = GetComponent<Animator>();
        anim = GetComponent<Animation>();

        rootTransform = this.transform;

        myClip = new AnimationClip();
        myClip.name = "TestAnimation";
        myClip.legacy = true;

        for (int i = 0; i < bi.boneList.Count; i++)
        {
            BVHImporter.Bone bone = bi.boneList[i];
            string bvhName = bone.name;
            if(!nameMap.ContainsKey(bvhName))
            {
                continue;
            }
            else
            {
                string targetName = nameMap[bvhName];
                Transform targetTransform = SearchTarget(rootTransform, targetName);
                path = GetRelativePath(rootTransform, targetTransform);
                SetCurve(bone, targetTransform);
            }
        }

        myClip.EnsureQuaternionContinuity();
        anim.AddClip(myClip, myClip.name);
        anim.Play(myClip.name);
    }

    private void SetCurve(BVHImporter.Bone bone, Transform targetTransform)
    {
        bool posX = false;
        bool posY = false;
        bool posZ = false;
        bool rotX = false;
        bool rotY = false;
        bool rotZ = false;

        string[] props = new string[7];
        keyframes = new Keyframe[7][];
        float[][] values = new float[6][];

        for (int channel = 0; channel < 6; channel++)
        {
            if (!bone.channels[channel].enabled)
            {
                continue;
            }

            switch (channel)
            {
                case 0:
                    posX = true;
                    props[channel] = "localPosition.x";
                    break;
                case 1:
                    posY = true;
                    props[channel] = "localPosition.y";
                    break;
                case 2:
                    posZ = true;
                    props[channel] = "localPosition.z";
                    break;
                case 3:
                    rotX = true;
                    props[channel] = "localRotation.x";
                    break;
                case 4:
                    rotY = true;
                    props[channel] = "localRotation.y";
                    break;
                case 5:
                    rotZ = true;
                    props[channel] = "localRotation.z";
                    break;
                default:
                    channel = -1;
                    break;
            }

            if (channel == -1)
            {
                continue;
            }

            keyframes[channel] = new Keyframe[frames];
            values[channel] = bone.channels[channel].value;
            if (rotX && rotY && rotZ && keyframes[6] == null)
            {
                keyframes[6] = new Keyframe[frames];
                props[6] = "localRotation.w";
            }
        }

        float time = 0.0f;
        if (posX && posY && posZ)
        {
            Vector3 offset;
            offset = new Vector3(-bone.offsets.x, bone.offsets.y, bone.offsets.z);

            for (int i = 0; i < frames; i++)
            {
                time += 1.0f / frameRate;
                keyframes[0][i].time = time;
                keyframes[1][i].time = time;
                keyframes[2][i].time = time;

                keyframes[0][i].value = -values[0][i];
                keyframes[1][i].value = values[1][i];
                keyframes[2][i].value = values[2][i];

                Vector3 bvhPosition = rootTransform.transform.InverseTransformPoint(new Vector3(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value) + this.transform.position + offset);
                bvhPosition = Resize(bvhPosition);
                keyframes[0][i].value = bvhPosition.x * this.transform.localScale.x;
                keyframes[1][i].value = bvhPosition.y * this.transform.localScale.y;
                keyframes[2][i].value = bvhPosition.z * this.transform.localScale.z;
            }

            myClip.SetCurve(path, typeof(Transform), props[0], new AnimationCurve(keyframes[0]));
            myClip.SetCurve(path, typeof(Transform), props[1], new AnimationCurve(keyframes[1]));
            myClip.SetCurve(path, typeof(Transform), props[2], new AnimationCurve(keyframes[2]));
        }

        time = 0.0f;
        if(rotX && rotY && rotZ)
        {
            Quaternion oldRotation = targetTransform.transform.rotation;
            for (int i = 0; i < frames; i++)
            {
                Vector3 bvhEuler = new Vector3(WrapAngle(values[3][i]), WrapAngle(values[4][i]), WrapAngle(values[5][i]));
                Quaternion rotation = Quaternion.Euler(bvhEuler);

                time += 1.0f / frameRate;
                keyframes[3][i].time = time;
                keyframes[4][i].time = time;
                keyframes[5][i].time = time;
                keyframes[6][i].time = time;

                keyframes[3][i].value = rotation.x;
                keyframes[4][i].value = -rotation.y;
                keyframes[5][i].value = -rotation.z;
                keyframes[6][i].value = rotation.w;

                targetTransform.transform.rotation = new Quaternion(keyframes[3][i].value, keyframes[4][i].value, keyframes[5][i].value, keyframes[6][i].value);
                keyframes[3][i].value = targetTransform.transform.localRotation.x;
                keyframes[4][i].value = targetTransform.transform.localRotation.y;
                keyframes[5][i].value = targetTransform.transform.localRotation.z;
                keyframes[6][i].value = targetTransform.transform.localRotation.w;
            }

            targetTransform.transform.rotation = oldRotation;
            myClip.SetCurve(path, typeof(Transform), props[3], new AnimationCurve(keyframes[3]));
            myClip.SetCurve(path, typeof(Transform), props[4], new AnimationCurve(keyframes[4]));
            myClip.SetCurve(path, typeof(Transform), props[5], new AnimationCurve(keyframes[5]));
            myClip.SetCurve(path, typeof(Transform), props[6], new AnimationCurve(keyframes[6]));
        }
    }

    public float WrapAngle(float angle)
    {
        if(angle > 180.0f)
        {
            return angle - 360.0f;
        }
        else if(angle < -180.0f)
        {
            return angle + 360.0f;
        }
        else
        {
            return angle;
        }
    }

    public Quaternion FromEulerYXZ(Vector3 euler)
    {
        return Quaternion.AngleAxis(euler.y, Vector3.up) * Quaternion.AngleAxis(euler.x, Vector3.right) 
            * Quaternion.AngleAxis(euler.z, Vector3.forward);
    }

    public Transform SearchTarget(Transform root, string target)
    {
        Transform result = null;
        if(root.name == target)
        {
            return root;
        }
        else if(root.childCount != 0)
        {
            for(int i = 0; result == null && i < root.childCount; i++)
            {
                result = SearchTarget(root.GetChild(i), target);
            }
            return result;
        }
        else
        {
            return result;
        }
    }

    public Vector3 Resize(Vector3 vec)
    {
        float size = 100.0f;
        return new Vector3(vec.x / size, vec.y / size, vec.z / size);
    }

    public string GetRelativePath(Transform root, Transform target)
    {
        string result = string.Empty;
        if(target == root)
        {
            return result;
        }

        else if(target.parent == root)
        {
            result += target.name;
            return result;
        }
        else if (target.parent != root)
        {
            result += GetRelativePath(root, target.parent);
            result += "/" + target.name;
            return result;
        }
        else
        {
            throw new InvalidOperationException("No path between root " + root.name + " and target " + target.name + "!");
        }
    }

    [CustomEditor(typeof(BVHAnimation))]
    public class TestAnimatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BVHAnimation bvhAnim = (BVHAnimation)target;

            if (GUILayout.Button("Read"))
            {
                bvhAnim.Read();
            }

            if (GUILayout.Button("Remap"))
            {
                bvhAnim.remap = new BVHAnimation.Remap[bvhAnim.bi.getCount() - 1];
                for (int i = 0; i < bvhAnim.bi.getCount() - 1; i++)
                {
                    string boneName = bvhAnim.bi.boneList[i].name;
                    if(boneName.Contains("LeftHand") || boneName.Contains("RightHand"))
                    {
                        if(boneName == "LeftHand" && boneName == "RightHand")
                        {
                            bvhAnim.remap[i].bvhName = boneName;
                            bvhAnim.remap[i].targetName = string.Empty;
                        }
                        else
                        {
                            bvhAnim.remap[i].bvhName = "Ignore";
                            bvhAnim.remap[i].targetName = "Ignore";
                        }
                    }
                    bvhAnim.remap[i].bvhName = boneName;
                    bvhAnim.remap[i].targetName = string.Empty;
                }
            }
        }
    }
}
