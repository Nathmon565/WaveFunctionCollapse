using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [Space(10)]

    ///<summary>Force the dungeon to generate</summary>
    public bool generateDungeon = false;

    [Header("Current Dungeon")]
    ///<summary>An empty object to hold all of the tiles and details of the dungeon</summary>
    public GameObject dungeonRoot;
    ///<summary>The list of instantiated prefabs, currently present in the world</summary>
    public List<GameObject> dungeonTiles;
    ///<summary>The list of instantiated prefabs, currently present in the world</summary>
    public List<GameObject> incompleteTiles;
    [Header("Camera")]
    ///<summary>A reference to the camera controller</summary>
    public GameObject cameraController;
    [Header("Debug")]
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
        Debug.Log("Generating the dungeon...\nWidth: " + dungeonDimensions.x + ", Length: " + dungeonDimensions.y + ", Tiles: " + (dungeonDimensions.x * dungeonDimensions.y));
        float timeStart = Time.realtimeSinceStartup;
        
        //Place the incomplete tiles
        CreateIncompleteTiles();
        Debug.Log("Incomplete tile placement complete - time taken: " + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100000) / 100f) + "ms (" + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100) / 100f) + " seconds)");

        //Start the Wave Function Collapse
        WFC();
        Debug.Log("Dungeon generation complete! Total time taken: " + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100000) / 100f) + "ms (" + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100) / 100f) + " seconds) ~" + (Mathf.Round((Time.realtimeSinceStartup - timeStart) / (dungeonDimensions.x * dungeonDimensions.y) * 100000) / 100f) + "ms/tile");
    }

    ///<summary>Create an instantiated and prepared version of the tile to be duplicated</summary>
    ///<param name="tile">The tile to be optimized</param>
    ///<param name="posModifier">The x coordinate modifier to esnure the raycast is not blocked</param>
    public GameObject OptimizeTile(GameObject tile, Transform parent, float posModifier) {
        GameObject newTile = Instantiate(tile, new Vector3(posModifier, 0, 0), Quaternion.Euler(Vector3.zero), parent);
        //Run the direction availibility function
        newTile.GetComponent<DungeonTile>().SetDirectionAvailability();
        //If this is has TileCreation, meaning it's the incomplete tile
        if(newTile.GetComponent<TileCreation>()) {
            TileCreation tileCreation = newTile.GetComponent<TileCreation>(); 
            //Add the other tiles as booleans to the tile list
            for(int i = 0; i < tiles.ToArray().Length; i++) {
                //Make a new possible tile
                TileCreation.PossibleTiles possibleTile = new TileCreation.PossibleTiles();
                //Get a reference to directions
                DungeonTile.AvailableDirections directions = tiles[i].GetComponent<DungeonTile>().directions;
                //If all 4 directions are true (It's an X)
                if(directions.localXPos && directions.localXNeg && directions.localZPos && directions.localZNeg) {
                    //X can only have one rotation
                    possibleTile.availableRotations = new List<bool> {true, false, false, false};
                }
                //If only 2 opposite x directions are true (It's an I)
                else if(directions.localXPos && directions.localXNeg && !directions.localZPos && !directions.localZNeg) {
                    //I can only have 2 rotations
                    possibleTile.availableRotations = new List<bool> {true, true, false, false};
                }
                //If no directions are true (it's a blank tile)
                else if(!directions.localXPos && !directions.localXNeg && !directions.localZPos && !directions.localZNeg) {
                    //Empty can only have 1 rotation
                    possibleTile.availableRotations = new List<bool> {true, false, false, false};
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
        for(int x = 0; x < dungeonDimensions.x; x++) {
            for(int y = 0; y < dungeonDimensions.y; y++) {
                //Create an empty tile, and add it to the list of tiles
                GameObject tile = Instantiate(defaultTile, new Vector3(x * 10 + 5, 0, y * 10 + 5), Quaternion.Euler(Vector3.zero), dungeonRoot.transform);
                dungeonTiles.Add(tile);
                //Set the index of the tile as the length of the array - 1
                tile.GetComponent<TileCreation>().index = dungeonTiles.ToArray().Length - 1;
            }
        }
    }

    ///<summary>Wave Function Collapse: select a tile, then start knocking out choices. Rinse and repeat</summary>
    private void WFC() {
        //Keep track of whether we are done, to know when to stop
        bool doneGenerating = false;
        //Debug
        int tilesToRun = runTiles;
        if(runTiles == -1) {
            tilesToRun = (int)(dungeonDimensions.x * dungeonDimensions.y);
        }
        //Keep track of the incomplete tiles
        incompleteTiles = dungeonTiles;

        while(!doneGenerating) {
            //Select a random incomplete tile with the highest entropy
            GameObject targetTileObj = FindMaxEntropy(incompleteTiles);
            //Check to make sure we actually have a target
            if(targetTileObj != null) {
                //Get a reference to the TileCreation
                TileCreation targetTile = targetTileObj.GetComponent<TileCreation>();

                //Force the first tile to be chosen
                ChooseRandomTileAndRotation(targetTile.possibleTiles);

                bool knockingOut = true;
                
                while(knockingOut) {
                    //TODO continue knocking out options until nothing can be chosen

                    //TEMP debug force exit after a certain number of tiles
                    tilesToRun--;
                    if(tilesToRun <= 0) {
                        doneGenerating = true;
                    }
                    
                    //if nothing else can be done, exit
                    knockingOut = false;
                }
                
            }

            //Pop any completed tiles from the list of incomplete tiles, setting them as their chosen tile
            incompleteTiles = PopTiles(incompleteTiles);
            //If there are no more incomplete tiles, end the loop.
            if(incompleteTiles.ToArray().Length == 0) { doneGenerating = true; }
        }
    }

    ///<summary>Search through the list of incomplete tiles and finalize tiles that only have one possibility</summary>
    private List<GameObject> PopTiles(List<GameObject> tileList) {
        //Create a new list of tiles
        List<GameObject> newTileList = tileList;

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
                    //Subtract 1 from the index of all of the following tiles
                    for(int j = i + 1; j < tileList.ToArray().Length; j++) {
                        tileList[j].GetComponent<TileCreation>().index--;
                    }
                    //Create the tile
                    tileList[i].GetComponent<TileCreation>().ChangeTile(tiles[tileChoice], Quaternion.Euler(new Vector3(0, rotationChoice * 90, 0)));
                    //Remove it from the list
                    newTileList.RemoveAt(i);
                    
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
                    //Set the max entropy as this tile
                    maxEntropy = tile.entropy;
                    //Add the 
                    targetTiles.Add(tileObj);
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
        //Keep a list of which tile indecies are in that list
        List<int> possibleTileIndecies = new List<int>();
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
                possibleTileIndecies.Add(c);
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
        //For each possible tile, set availibility to false
        foreach(TileCreation.PossibleTiles tile in possibleTiles) {
            //For each available rotation
            for(int i = 0; i < tile.availableRotations.ToArray().Length; i++) {
                //Set the availibility to false
                tile.availableRotations[i] = false;
            }
            //Set the availibility to false
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
            //Set the availibility to true
            randomTile.SetAvailability();
        }
        
    }
}
