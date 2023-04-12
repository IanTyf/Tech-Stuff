using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;

public class MultiAnimPreview: EditorWindow {

	class Styles
	{
		public Styles()
		{
		}
	}
	static Styles s_Styles;

	protected class Anim
	{
		public GameObject gameObject;
		public AnimationClip clip;
		public float time;
		public bool selected;
		public bool isLooping;

		public Anim()
		{
			time = 0f;
			selected = true;
			isLooping = false;
		}
	}

	protected bool animationMode = false;
	protected bool isPlayingAll = false;
	protected float lastTime;

	protected List<Anim> anims = new List<Anim>();

	[MenuItem("Benbees/Multi-Anim Preview", false, 2000)]
	public static void DoWindow()
	{
		GetWindow<MultiAnimPreview>();
	}

	public void OnEnable()
	{
		anims.Add(new Anim());
		anims.Add(new Anim());
	}

	public void OnGUI()
	{
		if (s_Styles == null)
			s_Styles = new Styles();

		GUILayout.BeginHorizontal(EditorStyles.toolbar);

		EditorGUI.BeginChangeCheck();
		GUILayout.Toggle(AnimationMode.InAnimationMode(), "Animate", EditorStyles.toolbarButton);
		if (EditorGUI.EndChangeCheck())
		{
			ToggleAnimationMode();
			Debug.Log(AnimationMode.InAnimationMode());
		}

		if (GUILayout.Button("Add", EditorStyles.toolbarButton))
		{
			AddAnim();
		}

		if (GUILayout.Button("Delete", EditorStyles.toolbarButton))
		{
			DeleteAnim();
		}

		GUILayout.FlexibleSpace();

		if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
		{
			Reset();
		}


		EditorGUI.BeginChangeCheck();
		isPlayingAll = GUILayout.Toggle(isPlayingAll, "Play", EditorStyles.toolbarButton);
		if (EditorGUI.EndChangeCheck())
		{
			if (isPlayingAll)
			{
				PlayAll();
				Debug.Log("playing all");
			}
			else
			{
				StopPlayAll();
				Debug.Log("stopped playing all");
			}
		}

		GUILayout.EndHorizontal();
		
		EditorGUILayout.BeginVertical();

		foreach (Anim anim in anims)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = 55f;
			anim.selected = EditorGUILayout.Toggle("selected", anim.selected, new GUILayoutOption[]{}); // a toggle for whether this animation should be played
			anim.gameObject = EditorGUILayout.ObjectField(anim.gameObject, typeof(GameObject), true) as GameObject; // a field for selecting gameObject
			EditorGUILayout.EndHorizontal();
			if (anim.gameObject != null) // if a gameObject is selected, display another field for selecting animation clip
			{
				anim.clip = EditorGUILayout.ObjectField(anim.clip, typeof(AnimationClip), false) as AnimationClip;
				if (anim.clip != null) // if an animation clip is selected, display other settings
				{
					float startTime = 0.0f;
					float stopTime = anim.clip.length;

					EditorGUILayout.BeginHorizontal();
					anim.isLooping = EditorGUILayout.Toggle("looping", anim.isLooping); // a toggle for whether this clip should be looped when played
					anim.time = EditorGUILayout.Slider(anim.time, startTime, stopTime); // a slider to adjust the animation timeline
					EditorGUILayout.IntField(Mathf.FloorToInt(anim.time * 60), new GUILayoutOption[] {GUILayout.Width(30f)}); // a quick field to show current frame #
					EditorGUILayout.EndHorizontal();
				}
				else if (AnimationMode.InAnimationMode())	AnimationMode.StopAnimationMode();
			}

			DrawUILine(Color.gray);
		}

		EditorGUILayout.EndVertical();
	}

	void Update()
	{
		if(!focusedWindow.ToString().Equals(" (MultiAnimPreview)")) return;

		float timePassed = 0f;
		if (Time.realtimeSinceStartup > lastTime)
        {
			timePassed = Time.realtimeSinceStartup - lastTime;
			lastTime = Time.realtimeSinceStartup;
        }
		

		if (!EditorApplication.isPlaying && AnimationMode.InAnimationMode())
		{
			if (isPlayingAll)
			{

				// do this bc the first few frames are always super slow, and the animation would snap to 0.33 or something immediately, which is not good
				if (timePassed < 0.1f)
				{
					foreach (Anim anim in anims)
					{
						if (anim.clip != null && anim.selected)
                        {
							float oldT = anim.time; // save the time to check for animation event that should occur here

							anim.time += timePassed;
							if (anim.isLooping && anim.time >= anim.clip.length) anim.time = anim.time - anim.clip.length;

							foreach (AnimationEvent evt in anim.clip.events)
                            {
								if (evt.time >= oldT && evt.time < anim.time)
                                {
									// call the animation event
									Debug.Log(evt.functionName);
									anim.gameObject.SendMessage(evt.functionName);
									
                                }
                            }
                        }
					}
				}
				else
				{
					// do this bc for some reason the frame rate will stay at lowest if time remains zero.. no clue why
					foreach (Anim anim in anims)
					{
						if (anim.clip != null && anim.selected)
                        {
							float oldT = anim.time; // save the time to check for animation event that should occur here

							anim.time += 0.005f;
							if (anim.isLooping && anim.time >= anim.clip.length) anim.time = anim.time - anim.clip.length;

							foreach (AnimationEvent evt in anim.clip.events)
							{
								if (evt.time >= oldT && evt.time < anim.time)
								{
									// call the animation event
									anim.gameObject.SendMessage(evt.functionName);
								}
							}
						}
					}
				}

				//Debug.Log(Time.deltaTime);
			}


			AnimationMode.BeginSampling();
			foreach (Anim anim in anims)
			{
				if (anim.gameObject == null || anim.clip == null) continue;

				Animator animator = anim.gameObject.GetComponent<Animator>();
				if (animator != null && animator.runtimeAnimatorController == null)
				{
					//do nothing
				}
				else
				{
					AnimationMode.SampleAnimationClip(anim.gameObject, anim.clip, anim.time);
				}
				Repaint();
			}
			AnimationMode.EndSampling();

			SceneView.RepaintAll();
			Repaint();
		}
	}

	void ToggleAnimationMode()
	{
		Debug.Log("animation mode toggled");
		if(AnimationMode.InAnimationMode())
			AnimationMode.StopAnimationMode();
		else
			AnimationMode.StartAnimationMode();
	}

	void PlayAll()
	{
		//time = 0f;
		//time2 = 0f;
		if (!AnimationMode.InAnimationMode())
        {
			AnimationMode.StartAnimationMode();
			Debug.Log("started playing all");
        }
	}

	void StopPlayAll()
	{
		//time = 0f;
		//time2 = 0f;
	}

	void Reset()
	{	
		/*
		time = 0f;
		time2 = 0f;
		*/
		foreach (Anim anim in anims)
		{
			anim.time = 0f;
		}

		AnimationMode.StopAnimationMode();
	}

	void AddAnim()
	{
		anims.Add(new Anim());
	}

	void DeleteAnim()
    {
		for (int i=anims.Count-1; i>=0; i--)
        {
			if (anims[i].selected)
            {
				anims.RemoveAt(i);
            }
        }
    }

	public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
	{
		Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
		r.height = thickness;
		r.y += padding / 2;
		r.x -= 2;
		r.width += 6;
		EditorGUI.DrawRect(r, color);
	}
}
