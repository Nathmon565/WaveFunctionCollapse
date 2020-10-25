using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>Keeps track of what tiles can be selected on this incomplete tile</summary>
public class TileCreation : MonoBehaviour {
    [System.Serializable]
    ///<summary>Stores the booleans of a tile at any of the 4 rotations</summary>
    public class PossibleTiles {
        ///<summary>The name of the tile layout</summary>
        public string name;
        ///<summary>Whether this tile is available?</summary>
        public bool available = true;
        ///<summary>Which rotations can this tile be in? 0, 90, 180, 270 along the y axis</summary>
        public List<bool> availableRotations = new List<bool> {true, true, true, true};

        ///<summary>Sets the avialable boolean based on if any rotations are valid</summary>
        public bool SetAvailability() {
            //Default available to false
            available = false;
            //For each rotation type
            foreach(bool b in availableRotations) {
                //If the boolean is true, set available to true
                if(b) { available = b; }
            }
            return available;
        }
    }

    ///<summary>A list of the different tiles, along with all of their booleans</summary>
    public List<PossibleTiles> possibleTiles;

    ///<summary>How many possibilities does this tile have?</summary>
    public int entropy;
	public GameObject entropySphere;

    [Header("References")]
    ///<summary>A reference to the dungeon generator</summary>
    public DungeonGenerator dungeonGenerator;

    ///<summary>Update the entropy value based on how many choices this tile has</summary>
    public void UpdateEntropy() {
        //Reset the value
        entropy = 0;
		int maxEntropy = 0;
        //For each tile
        foreach(PossibleTiles p in possibleTiles) {
            //For each rotation
            foreach(bool b in p.availableRotations) {
                //Add one entropy per each rotation
                if(b) { entropy++; }
				maxEntropy++;
            }
        }
        if(entropy == 0) {Debug.LogError("ENTROPY IS 0 FOR TILE " + GetComponent<DungeonTile>().index + "!");}
		if(entropySphere == null) {
            entropySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            entropySphere.transform.parent = transform;
            entropySphere.transform.localPosition = Vector3.zero;
        }
		entropySphere.transform.localScale = new Vector3(10, 10, 10) * entropy/maxEntropy;
    }

    ///<summary>Replace this tile with a target tile, replacing the tile in the dungeonTiles list as well</summary>
    ///<param name="tile">The tile to be copied</param>
    ///<param name="rot">The rotation of the tile</param>
    public GameObject ChangeTile(GameObject tile, Quaternion rot) {
        //Create the new tile, in the proper position and rotation
        GameObject newTile = Instantiate(tile, transform.position, rot, transform.parent);
        newTile.GetComponent<DungeonTile>().index = GetComponent<DungeonTile>().index;
		newTile.GetComponent<DungeonTile>().UpdateRotationConnections();
        //Set the new tile to replace this incomplete tile
        dungeonGenerator.dungeonTiles[dungeonGenerator.dungeonTiles.IndexOf(gameObject)] = newTile;
        
        dungeonGenerator.tilesGenerated++;
        //Destroy the incomplete tile
        Destroy(gameObject);
        return newTile;
    }

    ///<summary>Eliminates any tiles that wouldn't fit with the given direction and availability</summary>
    ///<param name="direction">Which GLOBAL direction to check</param>
    ///<param name="available">Whether the direction its checking with is available or not</param>
    public void CompareRotation(int direction, bool available) {
        //For each possible tile
        for(int i = 0; i < possibleTiles.ToArray().Length; i++) {
            //For each rotation of that tile
            for(int j = 0; j < possibleTiles[i].availableRotations.ToArray().Length; j++) {
                //The directions at which this tile is available
                List<bool> localDirections = dungeonGenerator.tiles[i].GetComponent<DungeonTile>().localDirections;
                //Whether the tile at the direction will fit or not
                bool availableRotation;

                
                
                //If direction is less than j, then subtract
                if(direction < j) {
					//availableRotation = (localDirections[(int)Mathf.Repeat(Mathf.Abs(direction - j), 4)]);
                    availableRotation = (localDirections[(int)Mathf.Repeat(direction - j, 4)]);
                } else {
                    //Otherwise, add
                    availableRotation = (localDirections[(int)Mathf.Repeat(direction + j, 4)]);
                }
				availableRotation = (localDirections[(int)Mathf.Repeat(direction - j, 4)]);

				if(possibleTiles[i].availableRotations[j]) {
                    Debug.Log("Tile: " + possibleTiles[i].name + ", Dir: " + direction + ", J: " + j + ", ldir[" + direction + "-" + j + "]: " + localDirections[(int)Mathf.Repeat(direction - j, 4)] + ", ldir[" + direction + "+" + j + "]: " + localDirections[(int)Mathf.Repeat(direction + j, 4)] + 
                    "\nldirs[" + localDirections[0] + ", " + localDirections[1] + ", " + localDirections[2] + ", " + localDirections[3] + "] | (availableRotation) " + availableRotation + " != " + available + " = " + (available != availableRotation));
                }

                if(possibleTiles[i].availableRotations[j] && availableRotation != available) {
                    //If not, set it to false
                    possibleTiles[i].availableRotations[j] = false;
                    Debug.Log("Removed rotation " + j + " of tile " + possibleTiles[i].name + " from tile " + dungeonGenerator.dungeonTiles.IndexOf(gameObject));
                }
            }
            //Update the overall availability of that tile
            bool a = possibleTiles[i].SetAvailability();
            if(!a) { Debug.Log("Tile possibility " + possibleTiles[i].name + " of tile " + dungeonGenerator.dungeonTiles.IndexOf(gameObject) + " is no longer available (all rotations eliminated)"); }
        }
        UpdateEntropy();
    }
}
