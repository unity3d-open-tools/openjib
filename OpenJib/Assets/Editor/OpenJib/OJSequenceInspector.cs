using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof(OJSequence))]
public class OJSequenceInspector : Editor {
	private static float currentEditorTime;
	private static int currentEditorKeyframe;
	private static float timelineScale = 20;

	public override void OnInspectorGUI () {
		OJSequence sequence = (OJSequence) target;
		float padding = 10;
		float currentTime = sequence.playing ? currentEditorTime : sequence.currentTime;

		sequence.cam = (Camera) EditorGUILayout.ObjectField ( "Camera", sequence.cam, typeof ( Camera ), true );
		sequence.eventHandler = (GameObject) EditorGUILayout.ObjectField ( "Event handler", sequence.eventHandler, typeof ( GameObject ), true );

		if ( !sequence.cam ) {
			EditorGUILayout.LabelField ( "Please link a camera first" );
			return;
		}

		EditorGUILayout.Space ();
		
		// Playback controls
		EditorGUILayout.BeginHorizontal ();
		
		if ( sequence.playing ) {
			if ( GUILayout.Button ( "STOP" ) ) {
				sequence.Stop ();
			}
		
		} else {
			if ( GUILayout.Button ( "PLAY" ) ) {
				sequence.Play ();
			}
		}
		
		if ( GUILayout.Button ( "RESET" ) ) {
			currentEditorTime = 0;
			currentTime = 0;
			sequence.Reset ();
		}

		EditorGUILayout.EndHorizontal ();

		sequence.autoPlay = EditorGUILayout.Toggle ( "Autoplay", sequence.autoPlay );
		sequence.rotateAlongCurve = EditorGUILayout.Toggle ( "Rotate along curve", sequence.rotateAlongCurve );

		EditorGUILayout.Space ();

		timelineScale = EditorGUILayout.Slider ( "Scale", timelineScale + 80, 100, 500 ) - 80;
		
		// Timeline
		EditorGUILayout.BeginHorizontal ();
		
		GUILayout.Box ( "", GUILayout.Height ( 50 ), GUILayout.ExpandWidth ( true ) );
		
		Rect rect = GUILayoutUtility.GetLastRect ();

		if ( !sequence.playing && GUILayout.Button ( "+", GUILayout.Width ( 32 ), GUILayout.ExpandHeight ( true ) ) ) {
			currentEditorKeyframe = sequence.AddKeyframe ( currentTime );
			return;
		}
		
		EditorGUILayout.EndHorizontal ();

		GUI.BeginScrollView ( rect, new Vector2 ( currentTime * timelineScale, 0 ), new Rect ( 0, 0, padding * 2 + sequence.length * timelineScale, 20 ), GUIStyle.none, GUIStyle.none );
		
		for ( int s = 0; s <= sequence.length; s++ ) {
			GUI.Label ( new Rect ( padding - 5 + s * timelineScale, 0, 20, 14 ), s.ToString() );
		}

		GUI.Box ( new Rect ( padding, 34, sequence.length * timelineScale, 2 ), "" );

		GUI.color = Color.red;

		GUI.Box ( new Rect ( padding + currentTime * timelineScale, 20, 2, 30 ), "" );

		for ( int i = 0; i < sequence.keyframes.Count; i++ ) {
			OJKeyframe kf = sequence.keyframes [ i ];

			GUI.color = i == currentEditorKeyframe ? Color.green : Color.white;

			if ( GUI.Button ( new Rect ( padding - 5f + kf.time * timelineScale, 25.0f, 10f, 20f ), "" ) ) {
				currentEditorKeyframe = i;
				return;
			}
		}

		GUI.color = Color.white;
		
		GUI.EndScrollView ();

		if ( !sequence.playing ) {
			// Scrobble
			EditorGUILayout.Space ();

			EditorGUILayout.BeginHorizontal ();
			currentTime = EditorGUILayout.Slider ( currentTime, 0, sequence.length );
			EditorGUILayout.LabelField ( "/", GUILayout.Width ( 10 ) );
			sequence.length = EditorGUILayout.FloatField ( sequence.length, GUILayout.Width ( 50 ) );
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			if ( currentEditorKeyframe > -1 && currentEditorKeyframe < sequence.keyframes.Count ) { 
				OJKeyframe kf = sequence.keyframes [ currentEditorKeyframe ];
				EditorGUILayout.LabelField ( "# " + ( currentEditorKeyframe + 1 ) + " / " + sequence.keyframes.Count );
				
				kf.stop = EditorGUILayout.Toggle ( "Stop", kf.stop );
				kf.time = EditorGUILayout.Slider ( "Time", kf.time, 0, sequence.length );
				
				EditorGUILayout.Space ();
				
				// Event
				EditorGUILayout.LabelField ( "Event", EditorStyles.boldLabel );
				kf.evt.message = EditorGUILayout.TextField ( "Message", kf.evt.message );
				kf.evt.argument = EditorGUILayout.TextField ( "Argument", kf.evt.argument );

				EditorGUILayout.Space ();
				
				// Transform
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ( "Transform", EditorStyles.boldLabel );
				if ( GUILayout.Button ( "Copy from scene", GUILayout.Width ( 120 ) ) ) {
					kf.position = sequence.cam.transform.position - sequence.transform.position;
					kf.rotation = sequence.cam.transform.localEulerAngles;
				}
				EditorGUILayout.EndHorizontal ();
				
				kf.position = EditorGUILayout.Vector3Field ( "Position", kf.position );
				kf.rotation = EditorGUILayout.Vector3Field ( "Rotation", kf.rotation );
				
				// Curve
				EditorGUILayout.Space ();
				EditorGUILayout.LabelField ( "Curve", EditorStyles.boldLabel );
				kf.curve.symmetrical = EditorGUILayout.Toggle ( "Symmetrical", kf.curve.symmetrical );
				kf.curve.before = EditorGUILayout.Vector3Field ( "Before", kf.curve.before );
				kf.curve.after = EditorGUILayout.Vector3Field ( "After", kf.curve.after );

				EditorGUILayout.Space ();
			
				// Properties	
				EditorGUILayout.LabelField ( "Properties", EditorStyles.boldLabel );
				kf.fov = (int)EditorGUILayout.Slider ( "FOV", kf.fov, 1, 179 );
				kf.brightness = EditorGUILayout.Slider ( "Brightness", kf.brightness, 0, 1 );
				
				// Actions
				EditorGUILayout.Space ();

				if ( GUILayout.Button ( "Remove" ) ) {
					sequence.RemoveKeyframe ( currentEditorKeyframe );
				}
			}
		}

		// Make sure the list of keyframes is sorted by time
		if ( !sequence.playing && GUI.changed ) {
			sequence.SetTime ( currentTime );
		}

		if ( !sequence.playing ) {
			currentEditorTime = currentTime;
		}
	}

	private void DrawHandles ( OJSequence sequence, OJKeyframe kf ) {
		if ( kf.curve.before != Vector3.zero ) {
			Handles.DrawLine ( sequence.transform.position + kf.position, sequence.transform.position + kf.position + kf.curve.before );
		}
		
		if ( kf.curve.after != Vector3.zero ) {
			Handles.DrawLine ( sequence.transform.position + kf.position, sequence.transform.position + kf.position + kf.curve.after );
		}
	}

	public void OnSceneGUI () {
		OJSequence sequence = (OJSequence) target;
	
		if ( currentEditorKeyframe >= sequence.keyframes.Count ) {
			currentEditorKeyframe = 0;
		}

		if ( sequence.keyframes.Count > 1 ) {
			for ( int k = 1; k < sequence.keyframes.Count; k++ ) {
				Handles.color = new Color ( 1f, 1f, 1f, 0.5f );

				OJKeyframe kf1 = sequence.keyframes [ k - 1 ];
				OJKeyframe kf2 = sequence.keyframes [ k ];

				for ( float t = 0.05f; t <= 1.05f; t += 0.05f ) {
					Vector3 p1 = OJSequence.CalculateBezierPoint ( t - 0.05f, sequence.transform.position + kf1.position, sequence.transform.position + kf1.position + kf1.curve.after, sequence.transform.position + kf2.position + kf2.curve.before, sequence.transform.position + kf2.position );
					Vector3 p2 = OJSequence.CalculateBezierPoint ( t, sequence.transform.position + kf1.position, sequence.transform.position + kf1.position + kf1.curve.after, sequence.transform.position + kf2.position + kf2.curve.before, sequence.transform.position + kf2.position );
					
					Handles.DrawLine ( p1, p2 );
				}
			}
		
			OJKeyframe kf = sequence.keyframes [ currentEditorKeyframe ];
			
			Handles.color = new Color ( 0f, 1f, 1f, 0.5f );
			DrawHandles ( sequence, kf );
		
			Vector3 before = kf.curve.before;
			kf.curve.before = Handles.PositionHandle ( sequence.transform.position + kf.position + kf.curve.before, Quaternion.Euler ( Vector3.zero ) ) - kf.position - sequence.transform.position;
			
			Vector3 after = kf.curve.after;
			kf.curve.after = Handles.PositionHandle ( sequence.transform.position + kf.position + kf.curve.after, Quaternion.Euler ( Vector3.zero ) ) - kf.position - sequence.transform.position;
		
			if ( kf.curve.symmetrical ) {	
				if ( before != kf.curve.before ) {
					kf.MirrorCurveAfter ();
				
				} else if ( after != kf.curve.after ) {
					kf.MirrorCurveBefore ();
				
				}
			}
		}
		
	}
}
