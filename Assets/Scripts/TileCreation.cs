using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>Keeps track of what tiles can be selected on this incomplete tile</summary>
public class TileCreation : MonoBehaviour {
    [System.Serializable]
    ///<summary>Stores the booleans of a tile at any of the 4 rotations</summary>
    public class PossibleTiles {
        ///<summary>Whether this tile is available?</summary>
        public bool available = true;
        ///<summary>Which rotations can this tile be in? 0, 90, 180, 270 along the y axis</summary>
        public List<bool> availableRotations = new List<bool> {true, true, true, true};

        ///<summary>Sets the avialable boolean based on if any rotations are valid</summary>
        public void SetAvailability() {
            //Default available to false
            available = false;
            //For each rotation type
            foreach(bool b in availableRotations) {
                //If the boolean is true, set available to true
                if(b) { available = b; }
            }
        }
    }

    ///<summary>A list of the different tiles, along with all of their booleans</summary>
    public List<PossibleTiles> possibleTiles;
    ///<summary>What location in the DungeonGenerator.dungeonTiles array are we in?</summary>
    public int index;

    ///<summary>How many possibilities does this tile have?</summary>
    public int entropy;

    [Header("References")]
    ///<summary>A reference to the dungeon generator</summary>
    public DungeonGenerator dungeonGenerator;

    ///<summary>Update the entropy value based on how many choices this tile has</summary>
    public void UpdateEntropy() {
        //Reset the value
        entropy = 0;
        //For each tile
        foreach(PossibleTiles p in possibleTiles) {
            //For each rotation
            foreach(bool b in p.availableRotations) {
                //Add one entropy per each rotation
                if(b) { entropy++; }
            }
        }
    }

    ///<summary>Replace this tile with a target tile, replacing the tile in the dungeonTiles list as well</summary>
    ///<param name="tile">The tile to be copied</param>
    ///<param name="rot">The rotation of the tile</param>
    public GameObject ChangeTile(GameObject tile, Quaternion rot) {
        //Create the new tile, in the proper position and rotation
        GameObject newTile = Instantiate(tile, transform.position, rot, transform.parent);
        Debug.Log("Removing tile index " + index);
        //Set the new tile to replace this incomplete tile
        dungeonGenerator.dungeonTiles[dungeonGenerator.dungeonTiles.IndexOf(gameObject)] = newTile;
        
        dungeonGenerator.tilesGenerated++;
        //Destroy the incomplete tile
        Destroy(gameObject);
        return newTile;
    }

    ///<summary>Eliminates any tiles that wouldn't fit with the given direction and availability</summary>
    ///<param name="direction">Which LOCAL direction to check</summary>
    ///<param name="available">Whether the direction its checking with is available or not</summary>
    public void CompareRotation(int direction, bool available) {
        //For each possible tile
        for(int i = 0; i < possibleTiles.ToArray().Length; i++) {
            //For each rotation of that tile
            for(int j = 0; j < possibleTiles[i].availableRotations.ToArray().Length; j++) {
                //If the rotated tile would fit in the direction of the availability
                if(dungeonGenerator.tiles[i].GetComponent<DungeonTile>().localDirections[(int)Mathf.Repeat(direction + j, 3)] != available) {
                    //If not, set it to false
                    possibleTiles[i].availableRotations[j] = false;
                }
                //Update the overall availability of that tile
                possibleTiles[i].SetAvailability();
            }
        }
        UpdateEntropy();
    }
}
