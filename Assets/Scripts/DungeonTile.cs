using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>Defines how this tile can be placed in relation to other tiles</summary>
public class DungeonTile : MonoBehaviour {
    ///<summary>What location in the DungeonGenerator.dungeonTiles array are we in?</summary>
    public int index;
    public bool readyToUse = false;
    ///<summary>What local directions this tile can connect to (up, right, down, left)</summary>
    public List<bool> localDirections;
	///<summary>What global directions this tile can connect to (local + rotation)</summary>
	public List<bool> globalDirections;
    ///<summary>How many faces each sub mesh has</summary>
    private int[] subMeshesFaceTotals;
    ///<summary>How many sub meshes there are</summary>
    private int totalSubMeshes;
	public bool updateRotationConnections;

	private void Update() {
		if(updateRotationConnections) {
			UpdateRotationConnections();
			updateRotationConnections = false;
		}
	}

    ///<summary>Set the available direction booleans based on the materials of the model at the cardinal directions</summary>
    public void SetDirectionAvailability() {
        //Credit to Damien O'Connell at
        //https://forum.unity.com/threads/detecting-material-material-index-and-raycast-hit.40377/
        //for detecting material from a raycast hit

        //Determine how many subMeshes and faces this has
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        
        //Get the number of sub meshes on this object
        totalSubMeshes = mesh.subMeshCount;
        
        //Set up the array to the number of sub meshes
        subMeshesFaceTotals = new int[totalSubMeshes];

        //For each mesh...
        for(int i = 0; i < totalSubMeshes; i++) {
            //Calculate the number of faces on this mesh
            subMeshesFaceTotals[i] = mesh.GetTriangles(i).Length / 3;
        }
        
        
        //Check and set the direction availability for each direction
        localDirections = new List<bool>(){
            CheckRayCastDirection(new Vector3(0, 1,  4.9f)),
			CheckRayCastDirection(new Vector3( 4.9f, 1, 0)),
			CheckRayCastDirection(new Vector3(0, 1, -4.9f)),
			CheckRayCastDirection(new Vector3(-4.9f, 1, 0))
        };

        readyToUse = true;
    }

    ///<summary>Sets the boolean according to originOffset and what the raycast hits</summary>
    ///<param name="originOffset">The local position that the raycast should originate from. Also used to determine which bool to change</param>
    private bool CheckRayCastDirection(Vector3 originOffset) {
        RaycastHit hit;
        if(Physics.Raycast(transform.TransformPoint(originOffset), -transform.up, out hit, 5)) {
            //Which sub mesh we hit
            int hitSubMeshNumber = 0;
            //The maximum number we can hit on the current submesh
            int maxVal = 0;
            
            //For each submesh...
            for(int i = 0; i < totalSubMeshes; i++) {
                //Add the number of triangles present
                maxVal += subMeshesFaceTotals[i];
                //If we are in range...
                if(hit.triangleIndex <= maxVal - 1) {
                    //Set the subMesh that we hit
                    hitSubMeshNumber = i + 1;
                    //Leave the for loop
                    break;
                }
                //Otherwise, try again
            }

            //If we hit the gray material (wall)
            if(hitSubMeshNumber == 1) {
                return false;
            } else {
                //We hit the white material (walkway)
                return true;
            }
        } else {
            Debug.LogError("Raycast failed when setting direction availability " + originOffset + " - It didn't hit anything.");
            return false;
        }
    }

	///<summary>Update the list of rotations for this tile if it's been rotated</summary>
	public void UpdateRotationConnections() {
		//If the rotation is not zero
		if(Mathf.Repeat(transform.localEulerAngles.y, 360) != 0) {
			List<bool> d = localDirections;
			//Find the rotation of this tile
			int i = Mathf.RoundToInt(Mathf.Repeat(transform.localEulerAngles.y, 360) / 90f);
			//Assign the rotations based on the offset local rotations
			globalDirections = new List<bool>{d[(int)Mathf.Repeat(i, 4)], d[(int)Mathf.Repeat(i + 1, 4)], d[(int)Mathf.Repeat(i + 2, 4)], d[(int)Mathf.Repeat(i + 3, 4)]};
			Debug.Log("i = " + i + ": " + Mathf.Repeat(i, 4) + " " + Mathf.Repeat(i + 1, 4) + " " +  Mathf.Repeat(i + 2, 4) + " " + Mathf.Repeat(i + 3, 4));
		}
		//If the tile isn't rotated, just use the global directions
		else { globalDirections = localDirections; }
	}
}
