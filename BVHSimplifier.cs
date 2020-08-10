#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

public class BVHSimplifier : MonoBehaviour
{
    public string BvhFileName;
    public string OutputFileName;
    public string Path;
    public void SimplifyData()
    {
        string[] lines = File.ReadAllLines("Assets\\" + BvhFileName);
        int index = 0;

        for (index = 0; index < lines.Length; index++)
        {
            if(lines[index].Contains("LeftHand") || lines[index].Contains("RightHand"))
            {
                index += 4;

                for (int i = 0; i < 14; i++)
                    ArrayExtensions.RemoveAt(ref lines, index);

                if(lines[index].Contains("Left"))
                    lines[index] = lines[index].Replace("JOINT\tCharacter1_LeftHandMiddle1", "End Site");
                else
                    lines[index] = lines[index].Replace("JOINT\tCharacter1_RightHandMiddle1", "End Site");

                lines[index + 3] = lines[index + 13];

                index += 4;

                for(int i = 0; i < 52; i++)
                    ArrayExtensions.RemoveAt(ref lines, index);
            }

            if (lines[index].Contains("MOTION"))
            {
                index += 3;
                break;
            }
        }

        for(; index < lines.Length; index++)
        {
            string[] entries = lines[index].Split('\t');
            string str = string.Empty;

            for(int i = 0; i < 30; i++)
                ArrayExtensions.RemoveAt(ref entries, 45);

            for (int i = 0; i < 30; i++)
                ArrayExtensions.RemoveAt(ref entries, 60);

            for(int i = 0; i < entries.Length - 1; i++)
                str += entries[i] + "\t";

            str += entries[entries.Length - 1];
            lines[index] = str;
        }

        string filePath = Application.dataPath + "\\" + Path + "\\" + OutputFileName;
        Debug.Log(filePath);

        using (var outputFile = new StreamWriter(filePath))
        {
            foreach (string line in lines)
                outputFile.WriteLine(line);
        }

        Debug.Log("Simplify Successfully!");
    }

    [CustomEditor(typeof(BVHSimplifier))]
    public class BVHAnimatorEditor : Editor
    {
        public BVHSimplifier Target;

        private void Awake()
        {
            Target = (BVHSimplifier)target;
        }

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(Target, Target.name);

            Target.BvhFileName = EditorGUILayout.TextField("Bvh file: Assets\\", Target.BvhFileName);
            Target.OutputFileName = EditorGUILayout.TextField("Output file name:", Target.OutputFileName);
            Target.Path = EditorGUILayout.TextField("Output path: Assets\\", Target.Path);

            if (GUILayout.Button("Simplify"))
            {
                Target.SimplifyData();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(Target);
            }
        }
    }
}
#endif 