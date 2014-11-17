using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OJBehaviour : MonoBehaviour {
	[System.Serializable]
	public class FloatRange {
		public float min;
		public float max;
	}
	
	[System.Serializable]
	public class Vector3Range {
		public Vector3 min;
		public Vector3 max;
	}

	public Camera cam;
	public bool autoPlay = false;
	public bool autoEdit = true;
	public bool smoothStep = false;
	public List< Transform > focus = new List< Transform > ();
	public Vector3 focusOffset;
	public float orbit;
	public Vector3Range position;
	public Vector3Range rotation;
	public float duration = 5;
	public bool playing = false;

	private float timer;

	public void Start () {
		playing = autoPlay;
	}

	public void NextShot () {
		NextShot ( duration );
	}

	public void NextShot ( float duration ) {
		this.duration = duration;

		cam.transform.position = new Vector3 (
			Random.Range ( position.min.x, position.max.x ),
			Random.Range ( position.min.y, position.max.y ),
			Random.Range ( position.min.z, position.max.z )
		);

		if ( focus.Count < 1 ) {
			cam.transform.localEulerAngles = rotation.min;
		}
		
		timer = 0;
	}

	public void Update () {
		if ( !cam ) {
			return;
		
		} else {
			cam.enabled = playing;

		}

		if ( autoEdit && timer >= duration ) {
			NextShot ();
		}

		float t = timer / duration;

		if ( focus.Count > 0 ) {
			Vector3 focusCenter = Vector3.zero;

			if ( focus.Count > 1 ) {
				int i = 0;
				
				for ( i = 0; i < focus.Count; i++ ) {
					focusCenter += focus[i].position;
				}

				focusCenter /= i;

			} else {
				focusCenter = focus[0].position; 
			
			}
			
			if ( orbit > 0 ) {
				cam.transform.RotateAround ( focusCenter, Vector3.up, ( orbit / duration ) * Time.deltaTime );
			}

			cam.transform.LookAt ( focusCenter + focusOffset );

		} else {
			if ( smoothStep ) {
				cam.transform.localEulerAngles = Vector3.Slerp ( rotation.min, rotation.max, t);
			} else {
				cam.transform.localEulerAngles = Vector3.Lerp ( rotation.min, rotation.max, t);
			}
		}
		
		timer += Time.deltaTime;
	}
}
