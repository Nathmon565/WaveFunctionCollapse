﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

///<summary>Handles the overall generation of the dungeon</summary>
public class DungeonGenerator : MonoBehaviour {
    [Header("Dungeon Settings")]
    ///<summary>How many tiles the dungeon spans along the world x and z axis</summary>
    public Vector2 dungeonDimensions = new Vector2(10, 10);
    ///<summary>The list of tile prefabs, to be instantiated</summary>
    public List<GameObject> tiles;
    ///<summary>The default tile to be placed when creating the dungeon</summary>
    public GameObject defaultTile;
    public GameObject tileRoot;
	[Range(0, 1)]
	public float stepDelay = 0;
    [Space(10)]

    ///<summary>Force the dungeon to generate</summary>
    public bool generateDungeon = false;

    [Header("Current Dungeon")]
    ///<summary>An empty object to hold all of the tiles and details of the dungeon</summary>
    public GameObject dungeonRoot;
    ///<summary>The list of instantiated prefabs, currently present in the world</summary>
    public List<GameObject> dungeonTiles;
    [Header("Camera")]
    ///<summary>A reference to the camera controller</summary>
    public GameObject cameraController;
	public TextMeshProUGUI statusText;
    [Header("Debug")]
	public Transform tileHighlight;
    public int runTiles = 1;
    public int tilesGenerated = 0;

    // Start is called before the first frame update
    private void Start() {
        //Create tileRoot if it doesn't exist
        if(tileRoot == null) {
            tileRoot = new GameObject();
            tileRoot.transform.position = Vector3.zero;
            tileRoot.name = "Tile Root";
        }

        //For each tile, optimize it so it is faster to instantiate
        for(int i = 0; i < tiles.ToArray().Length; i++) {
            tiles[i] = OptimizeTile(tiles[i], tileRoot.transform, (i + 1) * 10);
        }
        defaultTile = OptimizeTile(defaultTile, tileRoot.transform, 0);
        defaultTile.GetComponent<TileCreation>().dungeonGenerator = this;

        //Hide the optimized tiles
        tileRoot.SetActive(false);

        //Generate the dungeon with the current settings
        //once the scene is launched
        GenerateDungeon();
    }

    // Update is called once per frame
    private void Update() {
        if(generateDungeon) {
            GenerateDungeon();
            generateDungeon = false;
        }
    }

    ///<summary>Generate the dungeon with the current setup</summary>
    public void GenerateDungeon() {
        tilesGenerated = 0;
		statusText.text = "Pre-generation...";
        Debug.Log("Generating the dungeon...\nWidth: " + dungeonDimensions.x + ", Length: " + dungeonDimensions.y + ", Tiles: " + (dungeonDimensions.x * dungeonDimensions.y));
        float timeStart = Time.realtimeSinceStartup;
        
        //Place the incomplete tiles
        CreateIncompleteTiles();
		statusText.text = "Done placing incomplete tiles.";
        Debug.Log("Incomplete tile placement complete - time taken: " + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100000) / 100f) + "ms (" + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100) / 100f) + " seconds)");

        //Start the Wave Function Collapse
        StartCoroutine(WFC());
		statusText.text = "Done generating.";
        Debug.Log("Dungeon generation complete! Total time taken: " + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100000) / 100f) + "ms (" + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100) / 100f) + " seconds) ~" + (Mathf.Round((Time.realtimeSinceStartup - timeStart) / (dungeonDimensions.x * dungeonDimensions.y) * 100000) / 100f) + "ms/tile");
    }

    ///<summary>Create an instantiated and prepared version of the tile to be duplicated</summary>
    ///<param name="tile">The tile to be optimized</param>
    ///<param name="posModifier">The x coordinate modifier to ensure the raycast is not blocked</param>
    public GameObject OptimizeTile(GameObject tile, Transform parent, float posModifier) {
        GameObject newTile = Instantiate(tile, new Vector3(posModifier, 0, 0), Quaternion.Euler(Vector3.zero), parent);
        //Run the direction availability function
        newTile.GetComponent<DungeonTile>().SetDirectionAvailability();
        //If this is has TileCreation, meaning it's the incomplete tile
        if(newTile.GetComponent<TileCreation>()) {
            TileCreation tileCreation = newTile.GetComponent<TileCreation>(); 
            //Add the other tiles as booleans to the tile list
            for(int i = 0; i < tiles.ToArray().Length; i++) {
                //Make a new possible tile
                TileCreation.PossibleTiles possibleTile = new TileCreation.PossibleTiles();
                //Get a reference to directions
                List<bool> directions = tiles[i].GetComponent<DungeonTile>().localDirections;
                //If all 4 directions are true (It's an X)
                if(directions[0] && directions[1] && directions[2] && directions[3]) {
                    //X can only have one rotation
                    possibleTile.name = "X";
                    possibleTile.availableRotations = new List<bool> {true, false, false, false};
                }
                //If only 2 opposite x directions are true (It's an I)
                else if(!directions[0] && directions[1] && !directions[2] && directions[3]) {
                    //I can only have 2 rotations
                    possibleTile.name = "I";
                    possibleTile.availableRotations = new List<bool> {true, true, false, false};
                }
                //If only one direction is true (It's a C)
                else if(!directions[0] && directions[1] && !directions[2] && !directions[3]) {
                    //C can only have 1 rotation
                    possibleTile.name = "C";
                }
                //If no directions are true (it's a blank tile)
                else if(!directions[0] && !directions[1] && !directions[2] && !directions[3]) {
                    //Empty can only have 1 rotation
                    possibleTile.name = "O";
                    possibleTile.availableRotations = new List<bool> {true, false, false, false};
                }
                //If it's two adjacent directions
                else if(directions[1] && directions[2]) {
                    possibleTile.name = "L";
                }
                //If it's three directions
                else {
                    possibleTile.name = "T";
                }
                //Add the tile
                tileCreation.possibleTiles.Add(possibleTile);
            }
            newTile.GetComponent<TileCreation>().UpdateEntropy();
        }
        StartCoroutine(WaitUntilReady(newTile.GetComponent<DungeonTile>()));
        return newTile;
    }

    ///<summary>Waits until the tile's readyToUse boolean is true</summary>
    ///<param name="tile">The tile to check</param>
    private IEnumerator WaitUntilReady(DungeonTile tile) {
        yield return new WaitUntil(() => tile.readyToUse);
    }
    
    ///<summary>Iterate through the width and height settings to place all of the incomplete tiles in the dungeon</summary>
    private void CreateIncompleteTiles() {
		statusText.text = "Creating incomplete tiles...";
        //Place the camera
        cameraController.transform.position = new Vector3(dungeonDimensions.x * 5, 0, dungeonDimensions.y * 5);
        cameraController.transform.GetChild(0).localPosition = new Vector3(0, dungeonDimensions.x * 5 + dungeonDimensions.y * 5, 0);
        //If the dungeon has been generated before
        if(dungeonRoot != null) {
            //Destroy the dungeon, and reset the list
            Destroy(dungeonRoot);
            dungeonTiles = new List<GameObject>();
        }
        //Create a new root at (0,0,0)
        dungeonRoot = new GameObject();
        dungeonRoot.transform.position = Vector3.zero;
        dungeonRoot.name = "Dungeon Root";
        
        //Create the list of empty tiles
        for(int y = (int)dungeonDimensions.y; y > 0; y--) {
            for(int x = 0; x < dungeonDimensions.x; x++) {
                //Create an empty tile, and add it to the list of tiles
                GameObject tile = Instantiate(defaultTile, new Vector3(x * 10 + 5, 0, y * 10 - 5), Quaternion.Euler(Vector3.zero), dungeonRoot.transform);
                dungeonTiles.Add(tile);
                //Set the index of the tile as the length of the array - 1
                tile.GetComponent<DungeonTile>().index = dungeonTiles.ToArray().Length - 1;
            }
        }
    }

    ///<summary>Wave Function Collapse: select a tile, then start knocking out choices. Rinse and repeat</summary>
    private IEnumerator WFC() {
		statusText.text = "Starting WFC...";
        //Keep track of whether we are done, to know when to stop
        bool doneGenerating = false;
        //Debug
        int tilesToRun = runTiles;
        if(runTiles == -1) {
            tilesToRun = (int)(dungeonDimensions.x * dungeonDimensions.y);
        }

        while(!doneGenerating) {
            //Select a random incomplete tile with the highest entropy
            GameObject targetTileObj = FindMaxEntropy(dungeonTiles);
            int tileObjIndex = targetTileObj.GetComponent<DungeonTile>().index;
            //Debug.Log("Entropy: " + targetTileObj.GetComponent<TileCreation>().entropy);
			List<DungeonTile> selectedTile = new List<DungeonTile> {};
            //Check to make sure we actually have a target
            if(targetTileObj != null) {
                //Get a reference to the TileCreation
                TileCreation targetTile = targetTileObj.GetComponent<TileCreation>();

                if(targetTile == null) {
                    doneGenerating = true;
                    break;
                }

				

                //Force the first tile to be chosen
                statusText.text = "Observing a tile...";
                ChooseRandomTileAndRotation(targetTile.possibleTiles);
				targetTile.GetComponent<DungeonTile>().UpdateRotationConnections();

				targetTileObj = dungeonTiles[tileObjIndex];
                tileHighlight.position = targetTileObj.transform.position + new Vector3(0, 2, 0);
				if(!tileHighlight.gameObject.activeSelf) { tileHighlight.gameObject.SetActive(true); }
				if(stepDelay > 0) { yield return new WaitForSeconds(stepDelay); }

                dungeonTiles = PopTiles(dungeonTiles);
                targetTileObj = dungeonTiles[tileObjIndex];
				if(stepDelay > 0) { yield return new WaitForSeconds(stepDelay); }

				selectedTile.Add(targetTileObj.GetComponent<DungeonTile>());
				

                bool knockingOut = true;
                
                while(knockingOut) {
					statusText.text = "Knocking out nearby tiles...";
                    //For each cardinal direction of this tile
                    int tileIndex = selectedTile.ToArray().Length - 1;
                    if(stepDelay > 0) { yield return new WaitForSeconds(stepDelay); }
					for(int i = 0; i < 4; i++) {
						statusText.text = "Knocking out nearby tiles... (Checking rotation " + i + "...)";
						//Get a reference to an adjacent tile
						GameObject tileObj = FindAdjacentTile(targetTileObj.GetComponent<DungeonTile>(), i);
						//If it is an incomplete tile
						if(tileObj != null && tileObj.GetComponent<TileCreation>() != null && selectedTile.ToArray().Length >= 0) {
							//Get a tileCreation reference
							TileCreation tile = tileObj.GetComponent<TileCreation>();
                            //Remove the impossible rotations for the tile
							//tile.CompareRotation((int)Mathf.Repeat(i + 2, 4), selectedTile[tileIndex].globalDirections[(int)Mathf.Repeat(i + 2, 4)]);
							List<bool> globalDirections = selectedTile[tileIndex].globalDirections;
							List<bool> localDirections = selectedTile[tileIndex].localDirections;
                            Debug.LogWarning("Tile " + tileObjIndex + " Checking gdir[" + i + "], result " + selectedTile[tileIndex].globalDirections[(int)Mathf.Repeat(i, 4)] +
							"\ngdirs[" + globalDirections[0] + ", " + globalDirections[1] + ", " + globalDirections[2] + ", " + globalDirections[3] + "] ldirs[" + localDirections[0] + ", " + localDirections[1] + ", " + localDirections[2] + ", " + localDirections[3] + "]");
                            tile.CompareRotation((int)Mathf.Repeat(i + 2, 4), selectedTile[tileIndex].globalDirections[i]);
                            //Add that tile to the queue
                            selectedTile.Add(tileObj.GetComponent<DungeonTile>());
						}
                        if(stepDelay > 0) { yield return new WaitForSeconds(stepDelay); }
					}
					statusText.text = "Knocking out nearby tiles... (Removing current tile...)";
                    //Remove the tile we were selecting, we've exhausted the possibilities
                    selectedTile.RemoveAt(tileIndex);
                    if(selectedTile.ToArray().Length >= 0) {
                        //if nothing else can be done, exit
                        knockingOut = false;
                    }
                    
                }
                if(stepDelay > 0) { yield return new WaitForSeconds(stepDelay); }
            }
			statusText.text = "Looping tile generation...";
            //Pop any completed tiles from the list of incomplete tiles, setting them as their chosen tile
            dungeonTiles = PopTiles(dungeonTiles);
			if(stepDelay > 0) { yield return new WaitForSeconds(stepDelay); }
            //If there are no more incomplete tiles, end the loop.
            if(FindMaxEntropy(dungeonTiles) == null) { doneGenerating = true; }
        }
		if(stepDelay > 0) { yield return new WaitForSeconds(stepDelay); }
        tileHighlight.gameObject.SetActive(false);
    }

    ///<summary>Search through the list of incomplete tiles and finalize tiles that only have one possibility</summary>
    private List<GameObject> PopTiles(List<GameObject> tileList) {
        //Create a new list of tiles
        List<GameObject> newTileList = tileList;
		int l = 0;
        //For each tile in the list
        for(int i = 0; i < tileList.ToArray().Length; i++) {
            //Make sure this tile has TileCreation
            if(newTileList[i].GetComponent<TileCreation>() != null) {
                //Keep track of how many choices the tile has
                int tileChoices = 0;
                //Keep track of which tile was chosen
                int tileChoice = -1;
                //Keep track of how many rotation choices the tile has
                int rotationChoices = 0;
                //Keep track of which rotation was chosen
                int rotationChoice = -1;

                //For each tile option
                for(int j = 0; j < tileList[i].GetComponent<TileCreation>().possibleTiles.ToArray().Length; j++) {
                    //If this tile is an option
                    if(tileList[i].GetComponent<TileCreation>().possibleTiles[j].available) {
                        //Add it as a choice, and record the number
                        tileChoices++;
                        tileChoice = j;
                    }
                    //For each rotation option (0, 90, 180, 270)
                    for(int k = 0; k < 4; k++) {
                        //If this rotation is an option
                        if(tileList[i].GetComponent<TileCreation>().possibleTiles[j].availableRotations[k]) {
                            //Add it as a choice, and record the number
                            rotationChoices++;
                            rotationChoice = k;
                        }
                    }
                }
                
                //If there was only one choice for both tile and rotation
                if(tileChoices == 1 && rotationChoices == 1) {
                    //Create the tile
                    tileList[i].GetComponent<TileCreation>().ChangeTile(tiles[tileChoice], Quaternion.Euler(new Vector3(0, rotationChoice * 90, 0)));
					l++;
                }
            }
        }
        //Return the new list
        return newTileList;
    }

    ///<summary>Finds a random tile with the highest entropy</summary>
    ///<param list="list">The list of incomplete tiles to chose from</param>
    private GameObject FindMaxEntropy(List<GameObject> list) {
        List<GameObject> targetTiles = new List<GameObject>();
        int maxEntropy = -1;
        foreach(GameObject tileObj in list) {
            //Get a reference for tileCreation
            TileCreation tile = tileObj.GetComponent<TileCreation>();
            //Make sure it exists first
            if(tile) {
                //If the tile's entropy is greater than or equal to the current highest entropy
                if(tile.entropy >= maxEntropy) {
                    //Add the tile to the list
                    targetTiles.Add(tileObj);
                    //If it is larger than maximum
                    if(tile.entropy > maxEntropy) {
                        //Set the max entropy as this tile's
                        maxEntropy = tile.entropy;
                        //We need to update the list and remove all of the smaller ones
                        //Create a removal list
                        List<TileCreation> tileRemovalList = new List<TileCreation>();
                        //Loop through the target tile list
                        foreach(GameObject tileR in targetTiles) {
                            TileCreation tileRT = tileR.GetComponent<TileCreation>();
                            //If it's lower than the max entropy
                            if(tileRT.entropy < maxEntropy) {
                                //Add it to the removal list
                                tileRemovalList.Add(tileRT);
                            }
                        }
                        //Remove the inadequate tiles from the list
                        foreach(TileCreation t in tileRemovalList) {
                            targetTiles.Remove(t.gameObject);
                        }
                    }
                }
            }
        }
        //Check to make sure there are actually objects in the list
        int len = targetTiles.ToArray().Length;
        if(len > 0) {
            //Return a random choice from the list
            return targetTiles[Random.Range(0, len - 1)];
        } else {
            //Return null because there's no objects
            return null;
        }
    }

    ///<summary>Randomly chooses a tile and its rotation from available options. This is used to force entropy to decrease by choosing a random tile</summary>
    ///<param name="incompleteTileTiles">The list of possible tiles on the incomplete tile</param>
    private void ChooseRandomTileAndRotation(List<TileCreation.PossibleTiles> incompleteTileTiles) {
        //Keep a list of possible tiles to choose from
        List<TileCreation.PossibleTiles> possibleTiles = new List<TileCreation.PossibleTiles>();
        //Keep a list of which tile indices are in that list
        List<int> possibleTileIndices = new List<int>();
        //Keep a list of which possible rotations to choose from
        List<int> availableRotations = new List<int>();
        //Keep a count variable
        int c = 0;
        //For each possible tile
        foreach(TileCreation.PossibleTiles tile in incompleteTileTiles) {
            //If it's available
            if(tile.available) {
                //Add it to the list
                possibleTiles.Add(tile);
                //Add which index number it is
                possibleTileIndices.Add(c);
            }
            c++;
        }
        //Choose a random tile
        int randomChoice = Random.Range(0, possibleTiles.ToArray().Length - 1);
        TileCreation.PossibleTiles randomTile = possibleTiles[randomChoice];
        
        //For each available rotation
        for(int i = 0; i < randomTile.availableRotations.ToArray().Length; i++) {
            //If the rotation is available
            if(randomTile.availableRotations[i]) {
                //Add it to the list of available rotations
                availableRotations.Add(i);
            }
        }
        
        //Choose a random rotation
        int randomRotation = availableRotations[Random.Range(0, availableRotations.ToArray().Length - 1)];

        //Apply the random choices to the original incomplete tile
        //For each possible tile, set availability to false
        foreach(TileCreation.PossibleTiles tile in possibleTiles) {
            //For each available rotation
            for(int i = 0; i < tile.availableRotations.ToArray().Length; i++) {
                //Set the availability to false
                tile.availableRotations[i] = false;
            }
            //Set the availability to false
            tile.SetAvailability();
        }
        //For each rotation of the tile we chose
        for(int i = 0; i < randomTile.availableRotations.ToArray().Length; i++) {
            //If it matches the choice, set it to true
            if(i == randomRotation) {
                randomTile.availableRotations[i] = true;
            }
            //Otherwise, set it to false
            else {
                randomTile.availableRotations[i] = false;
            }
            //Set the availability to true
            randomTile.SetAvailability();
        }
        
    }

    ///<summary>Find and return an adjacent tile</summary>
    ///<param name="tTile">Which tile are we looking from?</param>
    ///<param name="direction">Which direction to look in? 0 is up, 1 is right, 2 is down, 3 is left</param>
    public GameObject FindAdjacentTile(DungeonTile tTile, int direction) {
        int dungeonWidth = (int)dungeonDimensions.x;
        int totalTiles = dungeonTiles.ToArray().Length;
        //Debug.Log(tTile.index + ", limit " + dungeonTiles.ToArray().Length + ", width: " + dungeonWidth);
        if(direction == 0) {
            //Up
            //If our position is not on the top row
            if(tTile.index >= dungeonWidth) {
                //The tile is one row above us
                return dungeonTiles[tTile.index - dungeonWidth];
            } else {
                return null;
            }
        } else if(direction == 1) { 
            //Right
            //If our position is not on the right edge
            if((tTile.index + 1) % dungeonWidth != 0) {
                //The tile is one to our right
                return dungeonTiles[tTile.index + 1];
            } else {
                return null;
            }
        } else if(direction == 2) {
            //Down
            //If our position is not on the bottom row
            if(tTile.index <= totalTiles - 1 - dungeonWidth) {
                //The tile is one row below us
                return dungeonTiles[tTile.index + dungeonWidth];
            } else {
                return null;
            }
        } else if(direction == 3) {
            //Left
            //If our position is not on the left edge
            if(tTile.index % dungeonWidth != 0) {
                //The tile is one to our left
                return dungeonTiles[tTile.index - 1];
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
