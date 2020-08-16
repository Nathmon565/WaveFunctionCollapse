﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>Keeps track of what tiles can be selected on this incomplete tile</summary>
public class TileCreation : MonoBehaviour {
    ///<summary>Which tiles can this be? Directly correlates with DungeonGenerator.tiles</summary>
    public List<bool> availableTiles = new List<bool>();
    ///<summary>Which rotations can this tile be in? 0, 90, 180, 270 along the y axis</summary>
    public List<bool> availableRotations = new List<bool> {true, true, true, true};
    ///<summary>What location in the DungeonGenerator.dungeonTiles array are we in?</summary>
    public int index;

    ///<summary>How many possibilities does this tile have?</summary>
    public int entropy;

    [Header("References")]
    ///<summary>A reference to the dungeon generator</summary>
    public DungeonGenerator dungeonGenerator;

    public void UpdateEntropy() {
        entropy = 0;
        foreach(bool b in availableTiles) {
            if(b) { entropy++; }
        }
    }

    ///<summary>Replace this tile with a target tile, replacing the tile in the dungeonTiles list as well</summary>
    ///<param name="tile">The tile to be copied</param>
    ///<param name="rot">The rotation of the tile</param>
    public GameObject ChangeTile(GameObject tile, Quaternion rot) {
        //Create the new tile, in the proper position and rotation
        GameObject newTile = Instantiate(tile, transform.position, rot, transform.parent);
        //Set the new tile to replace this incomplete tile
        dungeonGenerator.dungeonTiles[index] = newTile;
        //Destroy the incomplete tile
        Destroy(gameObject);

        return newTile;
    }

    ///<summary>Find and return an adjacent tile</summary>
    ///<param name="direction">Which direction to look in? 0 is up, 1 is right, 2 is down, 3 is left</param>
    public GameObject FindAdjacentTile(int direction) {
        int dungeonWidth = (int)dungeonGenerator.dungeonDimensions.x;
        int dungeonTiles = (int)(dungeonGenerator.dungeonDimensions.x * dungeonGenerator.dungeonDimensions.y);
        if(direction == 0) {
            //Up
            //If our position is not on the top row
            if(index >= dungeonWidth) {
                //The tile is one row above us
                return dungeonGenerator.dungeonTiles[index - dungeonWidth];
            } else {
                return null;
            }
        } else if(direction == 1) { 
            //Right
            //If our position is not on the right edge
            if(index + 1 % dungeonWidth != 0) {
                //The tile is one to our right
                return dungeonGenerator.dungeonTiles[index + 1];
            } else {
                return null;
            }
        } else if(direction == 2) {
            //Down
            //If our position is not on the bottom row
            if(index <= dungeonTiles - 1 - dungeonWidth) {
                //The tile is one row below us
                return dungeonGenerator.dungeonTiles[dungeonTiles - 1 - dungeonWidth - index];
            } else {
                return null;
            }
        } else if(direction == 3) {
            //Left
            //If our position is not on the left edge
            if(index % dungeonWidth != 0) {
                //The tile is one to our left
                return dungeonGenerator.dungeonTiles[index - 1];
            } else {
                return null;
            }
        } else {
            //Invalid input
            Debug.LogError("direction (" + direction + ") is not between 0 and 3");
            return null;
        }
    }
}