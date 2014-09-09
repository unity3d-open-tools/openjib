#pragma strict

public class OJBehaviour extends MonoBehaviour {
	public class FloatRange {
		public var min : float;
		public var max : float;
	}
	
	public class Vector3Range {
		public var min : Vector3;
		public var max : Vector3;
	}

	public var cam : Camera;
	public var autoPlay : boolean = false;
	public var autoEdit : boolean = true;
	public var smoothStep : boolean = false;
	public var focus : List.< Transform > = new List.< Transform > ();
	public var focusOffset : Vector3;
	public var orbit : float;
	public var position : Vector3Range;
	public var rotation : Vector3Range;
	public var duration : float = 5;
	public var playing : boolean = false;

	private var timer : float;

	public function Start () {
		playing = autoPlay;
	}

	public function NextShot () {
		NextShot ( duration );
	}

	public function NextShot ( duration : float ) {
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

	public function Update () {
		if ( !cam ) {
			return;
		
		} else {
			cam.enabled = playing;

		}

		if ( autoEdit && timer >= duration ) {
			NextShot ();
		}

		var t : float = timer / duration;

		if ( focus.Count > 0 ) {
			var focusCenter : Vector3;

			if ( focus.Count > 1 ) {
				for ( var i : int = 0; i < focus.Count; i++ ) {
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
