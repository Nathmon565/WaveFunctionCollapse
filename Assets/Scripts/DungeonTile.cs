using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>Defines how this tile can be placed in relation to other tiles</summary>
public class DungeonTile : MonoBehaviour {

    ///<summary>A class containing the booleans which determine what directions this
    /// tile can connect to</summary>
    [System.Serializable]
    public class AvailableDirections {
        ///<summary>Whether this tile can connect on the local +X axis</summary>
        public bool localXPos;
        ///<summary>Whether this tile can connect on the local -X axis</summary>
        public bool localXNeg;
        ///<summary>Whether this tile can connect on the local +Z axis</summary>
        public bool localZPos;
        ///<summary>Whether this tile can connect on the local -Z axis</summary>
        public bool localZNeg;
    }

    public bool readyToUse = false;
    public AvailableDirections directions = new AvailableDirections();
    
    ///<summary>How many faces each sub mesh has</summary>
    private int[] subMeshesFaceTotals;
    ///<summary>How many sub meshes there are</summary>
    private int totalSubMeshes;
    
    // Awake is called before Start()
    private void Awake() {
        //Figure out which directions this tile can connect to
        SetDirectionAvailability();
    }

    // Start is called before the first frame update
    void Start() {
        
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
        directions.localXPos = CheckRayCastDirection(new Vector3( 4.9f, 1, 0));
        directions.localXNeg = CheckRayCastDirection(new Vector3(-4.9f, 1, 0));
        directions.localZPos = CheckRayCastDirection(new Vector3(0, 1,  4.9f));
        directions.localZNeg = CheckRayCastDirection(new Vector3(0, 1, -4.9f));

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
}
