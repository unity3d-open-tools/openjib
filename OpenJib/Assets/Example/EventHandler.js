#pragma strict

public class EventHandler extends MonoBehaviour {
	public var objects : Transform;

	public function SpinObjects ( args : String ) {
		var degrees : float = float.Parse ( args.Split ( ","[0] ) [ 0 ] );
		var seconds : float = float.Parse ( args.Split ( ","[0] ) [ 1 ] );

		StartCoroutine ( function () : IEnumerator {
			var t : float = 0;
			
			while ( t <= seconds ) {
				t += Time.deltaTime;
				objects.localRotation = Quaternion.Lerp ( objects.localRotation, Quaternion.Euler ( new Vector3 ( 0, degrees, 0 ) ), t / seconds );
				yield null;
			}
		} () );
	}
}
