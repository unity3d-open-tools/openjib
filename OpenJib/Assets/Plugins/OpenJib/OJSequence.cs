using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class OJKeyframe {
	[System.Serializable]
	public class Curve {
		public bool symmetrical = false;
		public Vector3 before;
		public Vector3 after;
	}

	[System.Serializable]
	public class Event {
		public System.Action action;
		public string message;
		public string argument;
		public bool fired = false;

		public void Fire ( GameObject eventHandler ) {
			if ( !Application.isPlaying ) { return; }
			
			if ( action != null ) {
				action ();
			
			} else if ( eventHandler && !string.IsNullOrEmpty ( message ) ) {
				if ( !string.IsNullOrEmpty ( argument ) ) {
					eventHandler.SendMessage ( message, argument, SendMessageOptions.DontRequireReceiver );

				} else {
					eventHandler.SendMessage ( message, SendMessageOptions.DontRequireReceiver );

				}
			}

			fired = true;
		}	
	}

	public float time = 0;
	public bool stop;
	public Event evt = new Event ();
	public Vector3 position;
	public Vector3 rotation;
	public Curve curve = new Curve ();
	public int fov = 60;
	public float brightness = 1;

	public void Focus ( Transform cam, Transform target ) {
		Vector3 lookPos = target.position - cam.position;
		lookPos.y = 0;
		
		rotation = Quaternion.LookRotation ( lookPos ).eulerAngles;
	}
	
	public void MirrorCurveBefore () {
		curve.before = -curve.after;
	}
	
	public void MirrorCurveAfter () {
		curve.after = -curve.before;
	}
}

public class OJSequence : MonoBehaviour {
	[System.Serializable]
	public class KeyframePair {
		public OJKeyframe kf1;
		public OJKeyframe kf2;

		public KeyframePair ( OJKeyframe kf1, OJKeyframe kf2 ) {
			this.kf1 = kf1;
			this.kf2 = kf2;
		}
	}

	public string cameraTag = "SequenceCamera";
	public bool autoPlay = false;	
	public bool rotateAlongCurve = false;
	public List< OJKeyframe > keyframes = new List< OJKeyframe > (); 
	public Camera cam;
	public float length = 30;
	public float currentTime;
	public bool playing = false;
	public GameObject eventHandler;

	private Texture2D fadeTex;	
	private GameObject fadePlane;
	private Material fadeMaterial; 

	private bool isReady {
		get { return Application.isPlaying && cam != null && keyframes.Count > 0; }
	}

	public static Vector3 CalculateBezierPoint ( float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3 ) {
		float u = 1 - t;
		float tt = t*t;
	  	float uu = u*u;
	  	float uuu = uu * u;
	  	float ttt = tt * t;
	 
	  	Vector3 p = uuu * p0;
	  	p += 3 * uu * t * p1;
	  	p += 3 * u * tt * p2;
	  	p += ttt * p3;
	 
	  	return p;
	}	
	
	public void Play () {
		if ( isReady ) {
			cam.enabled = true;
			playing = true;
			
			foreach ( OJKeyframe kf in keyframes ) {
				kf.evt.fired = false;
			}
		}
	}

	public void Reset () {
		currentTime = 0;

		foreach ( OJKeyframe kf in keyframes ) {
			kf.evt.fired = false;
		}
	}

	public void Start () {
		if ( autoPlay ) {
			Play ();
		
		} else {
			if ( !cam ) {
				GameObject go = GameObject.FindWithTag ( cameraTag );
				cam = go.GetComponent< Camera > ();
			}
			
			if ( cam ) {
				cam.enabled = false;
			}
		}
	}

	public void Stop () {
		playing = false;
		cam.enabled = false;
	}

	public void Exit () {
		cam.enabled = false;
	}

	public void RemoveKeyframe ( int i ) {
		keyframes.RemoveAt ( i );
	}
	
	public void LerpKeyframe ( OJKeyframe kf, OJKeyframe kf1, OJKeyframe kf2, float percent ) {
		kf.position = Vector3.Lerp ( kf1.position, kf2.position, percent ); 
	}	

	public void LerpCamera ( OJKeyframe kf1, OJKeyframe kf2, float t ) {
		cam.fieldOfView = Mathf.Lerp ( kf1.fov, kf2.fov, t );
		cam.transform.position = CalculateBezierPoint ( t, transform.position + kf1.position, transform.position + kf1.position + kf1.curve.after, transform.position + kf2.position + kf2.curve.before, transform.position + kf2.position );

		if ( rotateAlongCurve ) {
			cam.transform.LookAt ( CalculateBezierPoint ( t + 0.05f, transform.position + kf1.position, transform.position + kf1.position + kf1.curve.after, transform.position + kf2.position + kf2.curve.before, transform.position + kf2.position ) );

		} else {
			cam.transform.localRotation = Quaternion.Lerp ( Quaternion.Euler ( kf1.rotation ), Quaternion.Euler ( kf2.rotation ), t ); 
		
		}
		
		float alpha = 1 - ( Mathf.Lerp ( kf1.brightness, kf2.brightness, t ) );
		fadeTex.SetPixels ( new Color[] { 
			new Color ( 0, 0, 0, alpha ),
			new Color ( 0, 0, 0, alpha ),
			new Color ( 0, 0, 0, alpha ),
			new Color ( 0, 0, 0, alpha )
		} );
		fadeTex.Apply ();
		
	}	

	public void SetCamera ( OJKeyframe kf ) {
		cam.fieldOfView = kf.fov;
		
		fadeTex.SetPixels ( new Color[] { 
			new Color ( 0, 0, 0, 1 - kf.brightness ),
			new Color ( 0, 0, 0, 1 - kf.brightness ),
			new Color ( 0, 0, 0, 1 - kf.brightness ),
			new Color ( 0, 0, 0, 1 - kf.brightness )
		} );
		fadeTex.Apply ();
		
		cam.transform.position = transform.position + kf.position;
		cam.transform.localRotation = Quaternion.Euler ( kf.rotation );
	}

	public int AddKeyframe ( float time ) {
		int i = 0;
		
		// Check if keyframe exists at the given time
		for ( i = 0; i < keyframes.Count; i++ ) {
			if ( keyframes [ i ].time == time ) {
				return i;
			}
		}

		// Create new keyframe
		OJKeyframe kf = new OJKeyframe ();
		KeyframePair closest = FindClosestKeyframes ();
		
		if ( closest.kf1 != null && closest.kf2 != null ) {
			float cursor = GetCursorPosition ( closest.kf1, closest.kf2 );
			LerpKeyframe ( kf, closest.kf1, closest.kf2, cursor );
		
		} else if ( closest.kf1 != null ) {
			//kf = closest.kf1;

		} else if ( closest.kf2 != null ) {
			//kf = closest.kf2;

		}

		kf.time = time;
		
		keyframes.Add ( kf );

		// Return the correct index
		for ( i = 0; i < keyframes.Count; i++ ) {
			if ( keyframes [ i ].time == time ) {
				return i;
			}
		}

		return 0;
	}

	public float GetCursorPosition ( OJKeyframe kf1, OJKeyframe kf2 ) {
		float min = kf1.time;
		float cursor = currentTime;
		float max = kf2.time;
		
		return ( cursor - min ) / ( max - min );
	}

	public KeyframePair FindClosestKeyframes () {
		OJKeyframe kf1 = null;
		OJKeyframe kf2 = null;
	
		for ( int i = 0; i < keyframes.Count; i++ ) {
			OJKeyframe kf = keyframes [ i ];

			if ( kf.time == currentTime || ( kf.time < currentTime && ( kf1 == null || Mathf.Abs ( kf.time - currentTime ) < Mathf.Abs ( kf1.time - currentTime ) ) ) ) {
				kf1 = kf;
			
			} else if ( kf.time > currentTime && ( kf2 == null || Mathf.Abs ( kf.time - currentTime ) < Mathf.Abs ( kf2.time - currentTime ) ) ) {
				kf2 = kf;

			}
		}

		return new KeyframePair ( kf1, kf2 );
	}

	public void SetTime ( float time ) {
		if ( !fadePlane ) {
			fadePlane = GameObject.CreatePrimitive ( PrimitiveType.Quad );
			fadePlane.transform.parent = cam.transform;
			fadePlane.transform.localPosition = new Vector3 ( 0f, 0f, cam.nearClipPlane + 0.1f );
			fadePlane.transform.localEulerAngles = new Vector3 ( 0f, 0f, 0f );

			fadeTex = new Texture2D ( 2, 2 );
			fadeTex.SetPixels ( new Color[] {
				new Color ( 0, 0, 0, 0 ),
				new Color ( 0, 0, 0, 0 ),
				new Color ( 0, 0, 0, 0 ),
				new Color ( 0, 0, 0, 0 )
			} );
			fadeTex.Apply ();
			
			fadeMaterial = new Material ( Shader.Find ( "Unlit/Transparent" ) );
			fadeMaterial.mainTexture = fadeTex;
			
			fadePlane.GetComponent< MeshRenderer > ().material = fadeMaterial;
		
		} else {
			fadePlane.transform.localScale = new Vector3 ( cam.fieldOfView / 60, cam.fieldOfView / 60, cam.fieldOfView / 60 );

		}

		currentTime = time;

		KeyframePair closest = FindClosestKeyframes ();
	
		// Fire event
		if ( Application.isPlaying && playing && closest.kf1 != null && !closest.kf1.evt.fired ) {
			closest.kf1.evt.Fire ( eventHandler );
		}

		if ( closest.kf1 != null ) {
			if ( closest.kf2 != null ) { 
				LerpCamera ( closest.kf1, closest.kf2, GetCursorPosition ( closest.kf1, closest.kf2 ) ); 
			
			} else {
				SetCamera ( closest.kf1 ); 

			}
		}	
	}

	public void Update () {
		if ( playing ) {
			currentTime += Time.deltaTime;

			if ( currentTime > length ) {
				Stop ();
			
			} else {			
				SetTime ( currentTime );
			
			}
		}
	}
}
