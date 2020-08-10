#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[ExecuteInEditMode]
public class Skeleton : MonoBehaviour
{

	public int Index = 0;

	public bool InspectSkeleton = false;

	public bool DrawRoot = false;
	public bool DrawSkeleton = true;
	public bool DrawTransforms = false;

	//public int MaxHistory = 0;

	public float BoneSize = 0.025f;
	public Color BoneColor = UltiDraw.Cyan;
	public Color JointColor = UltiDraw.Mustard;

	public Bone[] Bones = new Bone[0];

	void Reset()
	{
		ExtractSkeleton();
	}

	void Awake()
	{
		if (Application.isPlaying)
		{
			for (int i = 0; i < Bones.Length; i++)
			{
				Bones[i].ComputeLength();
			}
		}
	}

	public Transform GetRoot()
	{
		return transform;
	}

	public Transform[] FindTransforms(params string[] names)
	{
		Transform[] transforms = new Transform[names.Length];
		for (int i = 0; i < transforms.Length; i++)
		{
			transforms[i] = FindTransform(names[i]);
		}
		return transforms;
	}

	public Transform FindTransform(string name)
	{
		Transform element = null;
		Action<Transform> recursion = null;
		recursion = new Action<Transform>((transform) => {
			if (transform.name == name)
			{
				element = transform;
				return;
			}
			for (int i = 0; i < transform.childCount; i++)
			{
				recursion(transform.GetChild(i));
			}
		});
		recursion(GetRoot());
		return element;
	}

	public void ExtractSkeleton()
	{
		ArrayExtensions.Clear(ref Bones);
		Action<Transform, Bone> recursion = null;
		recursion = new Action<Transform, Bone>((transform, parent) => {
			Bone bone = new Bone(this, transform, Bones.Length);
			ArrayExtensions.Add(ref Bones, bone);
			if (parent != null)
			{
				bone.Parent = parent.Index;
				ArrayExtensions.Add(ref parent.Childs, bone.Index);
			}
			parent = bone;
			for (int i = 0; i < transform.childCount; i++)
			{
				recursion(transform.GetChild(i), parent);
			}
		});
		recursion(GetRoot(), null);
	}

	public void ExtractSkeleton(Transform[] bones)
	{
		ArrayExtensions.Clear(ref Bones);
		Action<Transform, Bone> recursion = null;
		recursion = new Action<Transform, Bone>((transform, parent) => {
			if (System.Array.Find(bones, x => x == transform))
			{
				Bone bone = new Bone(this, transform, Bones.Length);
				ArrayExtensions.Add(ref Bones, bone);
				if (parent != null)
				{
					bone.Parent = parent.Index;
					ArrayExtensions.Add(ref parent.Childs, bone.Index);
				}
				parent = bone;
			}
			for (int i = 0; i < transform.childCount; i++)
			{
				recursion(transform.GetChild(i), parent);
			}
		});
		recursion(GetRoot(), null);
	}

	public void ExtractSkeleton(string[] bones)
	{
		ExtractSkeleton(FindTransforms(bones));
	}

	public void Draw()
	{
		Draw(BoneColor, JointColor, 1f);
	}

	public void Draw(Color boneColor, Color jointColor, float alpha)
	{
		UltiDraw.Begin();
		if (DrawRoot)
		{
			UltiDraw.DrawWiredSphere(GetRoot().position, GetRoot().rotation, 0.1f, UltiDraw.DarkRed, UltiDraw.Black);
			UltiDraw.DrawTranslateGizmo(GetRoot().position, GetRoot().rotation, 0.1f);
		}

		if (DrawSkeleton)
		{
			Action<Bone> recursion = null;
			recursion = new Action<Bone>((bone) => {
				if (bone.GetParent() != null)
				{
					UltiDraw.DrawBone(
						bone.GetParent().Transform.position,
						Quaternion.FromToRotation(bone.GetParent().Transform.forward, bone.Transform.position - bone.GetParent().Transform.position) * bone.GetParent().Transform.rotation,
						12.5f * BoneSize * bone.GetLength(), bone.GetLength(), boneColor
					);
				}
				UltiDraw.DrawSphere(bone.Transform.position, Quaternion.identity, 5f / 8f * BoneSize, jointColor);
				for (int i = 0; i < bone.Childs.Length; i++)
				{
					recursion(bone.GetChild(i));
				}
			});
			if (Bones.Length > 0)
			{
				recursion(Bones[0]);
			}
		}

		if (DrawTransforms)
		{
			Action<Bone> recursion = null;
			recursion = new Action<Bone>((bone) => {
				UltiDraw.DrawTranslateGizmo(bone.Transform.position, bone.Transform.rotation, 0.05f);
				for (int i = 0; i < bone.Childs.Length; i++)
				{
					recursion(bone.GetChild(i));
				}
			});
			if (Bones.Length > 0)
			{
				recursion(Bones[0]);
			}
		}
		UltiDraw.End();
	}

	public Bone FindBone(string name)
	{
		return Array.Find(Bones, x => x.GetName() == name);
	}

	void OnRenderObject()
	{
		Draw();
	}

	void OnDrawGizmos()
	{
		if (!Application.isPlaying)
		{
			OnRenderObject();
		}
	}

	[Serializable]
	public class Bone
	{
		public Skeleton Skeleton;
		public Transform Transform;
		public int Index;
		public int Parent;
		public int[] Childs;
		public float Length;

		public Bone(Skeleton skeleton, Transform transform, int index)
		{
			Skeleton = skeleton;
			Transform = transform;
			Index = index;
			Parent = -1;
			Childs = new int[0];
			Length = GetLength();
		}

		public string GetName()
		{
			return Transform.name;
		}

		public Bone GetParent()
		{
			return Parent == -1 ? null : Skeleton.Bones[Parent];
		}

		public Bone GetChild(int index)
		{
			return index >= Childs.Length ? null : Skeleton.Bones[Childs[index]];
		}

		public void SetLength(float value)
		{
			Length = Mathf.Max(float.MinValue, value);
		}

		public float GetLength()
		{
			return GetParent() == null ? 0f : Vector3.Distance(GetParent().Transform.position, Transform.position);
		}

		public void ComputeLength()
		{
			Length = GetLength();
		}

		public void ApplyLength()
		{
			if (GetParent() != null)
			{
				Transform.position = GetParent().Transform.position + Length * (Transform.position - GetParent().Transform.position).normalized;
			}
		}
	}

	[CustomEditor(typeof(Skeleton))]
	public class Actor_Editor : Editor
	{

		public Skeleton Target;

		void Awake()
		{
			Target = (Skeleton)target;
		}

		public override void OnInspectorGUI()
		{
			Undo.RecordObject(Target, Target.name);

			Target.Index = EditorGUILayout.IntField("Index", Target.Index);

			Target.DrawRoot = EditorGUILayout.Toggle("Draw Root", Target.DrawRoot);
			Target.DrawSkeleton = EditorGUILayout.Toggle("Draw Skeleton", Target.DrawSkeleton);
			Target.DrawTransforms = EditorGUILayout.Toggle("Draw Transforms", Target.DrawTransforms);

			GUI.backgroundColor = Color.grey;
			using (new EditorGUILayout.VerticalScope("Box"))
			{
				GUI.backgroundColor = Color.white;
				if (GUIButton("Skeleton", UltiDraw.DarkGrey, UltiDraw.White))
				{
					Target.InspectSkeleton = !Target.InspectSkeleton;
				}
				if (Target.InspectSkeleton)
				{
					EditorGUILayout.LabelField("Skeleton Bones: " + Target.Bones.Length);
					Target.BoneSize = EditorGUILayout.FloatField("Bone Size", Target.BoneSize);
					Target.JointColor = EditorGUILayout.ColorField("Joint Color", Target.JointColor);
					Target.BoneColor = EditorGUILayout.ColorField("Bone Color", Target.BoneColor);
					InspectSkeleton(Target.GetRoot(), 0);
				}
			}

			if (GUI.changed)
			{
				EditorUtility.SetDirty(Target);
			}
		}

		private void InspectSkeleton(Transform transform, int indent)
		{
			Bone bone = Target.FindBone(transform.name);
			GUI.backgroundColor = bone == null ? UltiDraw.LightGrey : UltiDraw.Mustard;
			using (new EditorGUILayout.HorizontalScope("Box"))
			{
				GUI.backgroundColor = Color.white;
				EditorGUILayout.BeginHorizontal();
				for (int i = 0; i < indent; i++)
				{
					EditorGUILayout.LabelField("|", GUILayout.Width(20f));
				}
				EditorGUILayout.LabelField("-", GUILayout.Width(20f));
				EditorGUILayout.LabelField(transform.name + " " + (bone == null ? string.Empty : "(" + bone.Index.ToString() + ")" + " " + "(" + bone.GetLength() + ")"), GUILayout.Width(250f), GUILayout.Height(20f));
				GUILayout.FlexibleSpace();
				if (GUIButton("Bone", bone == null ? UltiDraw.White : UltiDraw.DarkGrey, bone == null ? UltiDraw.DarkGrey : UltiDraw.White))
				{
					Transform[] bones = new Transform[Target.Bones.Length];
					//for (int i = 0; i < bones.Length; i++)
					//{
					//	bones[i] = Target.Bones[i].Transform;
					//}
					//if (bone == null)
					//{
					//	ArrayExtensions.Add(ref bones, transform);
					//	Target.ExtractSkeleton(bones);
					//}
					//else
					//{
					//	ArrayExtensions.Remove(ref bones, transform);
					//	Target.ExtractSkeleton(bones);
					//}
				}
				EditorGUILayout.EndHorizontal();
			}
			//for (int i = 0; i < transform.childCount; i++)
			//{
			//	InspectSkeleton(transform.GetChild(i), indent + 1);
			//}
		}
	}

	private static bool GUIButton(string label, Color backgroundColor, Color textColor)
	{
		GUIStyle style = new GUIStyle("Button");
		style.normal.textColor = textColor;
		style.alignment = TextAnchor.MiddleCenter;
		GUI.backgroundColor = backgroundColor;
		bool clicked = GUILayout.Button(label, style);
		GUI.backgroundColor = Color.white;
		return clicked;
	}
}

#endif
