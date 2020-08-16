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
    [Header("Debug")]
    public Material red;

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
        Debug.Log("Generating the dungeon...\nWidth: " + dungeonDimensions.x + ", Length: " + dungeonDimensions.y + ", Tiles: " + (dungeonDimensions.x * dungeonDimensions.y));
        float timeStart = Time.realtimeSinceStartup;
        
        //Place the incomplete tiles
        CreateIncompleteTiles();
        Debug.Log("Incomplete tile placement complete - time taken: " + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100000) / 100f) + "ms (" + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100) / 100f) + " seconds)");

        //Start the Wave Function Collapse
        //WFC();
        Debug.Log("Dungeon generation complete! Total time taken: " + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100000) / 100f) + "ms (" + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100) / 100f) + " seconds)");
    }

    ///<summary>Create an instantiated and prepared version of the tile to be duplicated</summary>
    ///<param name="tile">The tile to be optimized</param>
    ///<param name="posModifier">The x coordinate modifier to esnure the raycast is not blocked</param>
    public GameObject OptimizeTile(GameObject tile, Transform parent, float posModifier) {
        GameObject newTile = Instantiate(tile, new Vector3(posModifier, 0, 0), Quaternion.Euler(Vector3.zero), parent);
        //If this is has TileCreation
        if(newTile.GetComponent<TileCreation>()) {
            //Add the other tiles as booleans to the tile list
            for(int i = 0; i < tiles.ToArray().Length; i++) {
                //newTile.GetComponent<TileCreation>().possibleTiles.Add(new TileCreation.PossibleTiles());
                //TODO deprecate
                newTile.GetComponent<TileCreation>().availableTiles.Add(true);
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
                GameObject tile = Instantiate(defaultTile, new Vector3(x * 10, 0, y * 10), Quaternion.Euler(Vector3.zero), dungeonRoot.transform);
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

        //Keep track of the incomplete tiles
        List<GameObject> incompleteTiles = dungeonTiles;

        while(!doneGenerating) {
            //Select a random incomplete tile to start
            GameObject targetTile = incompleteTiles[Random.Range(0, incompleteTiles.ToArray().Length-1)];
            int targetIndex = targetTile.GetComponent<TileCreation>().index;

            //If there are no more incomplete tiles, end the loop.
            if(incompleteTiles.ToArray().Length == 0) { doneGenerating = true; }
        }
    }

    ///<summary>Search through the list of incomplete tiles and finalize tiles that only have one possibility</summary>
    private List<GameObject> PopTiles(List<GameObject> tileList) {
        List<GameObject> newTileList = tileList;
        //For each tile in the list
        for(int i = 0; i < tileList.ToArray().Length; i++) {
            //Keep track of how many choices the tile has
            int tileChoices = 0;
            //Keep track of which tile was chosen
            int tileChoice = -1;
            //For each tile options
            for(int j = 0; j < tileList[i].GetComponent<TileCreation>().availableTiles.ToArray().Length; j++) {
                //If this tile is an option
                if(tileList[i].GetComponent<TileCreation>().availableTiles[j]) {
                    //Add it as a choice, and record the number
                    tileChoices++;
                    tileChoice = j;
                }
            }
            //Keep track of how many rotation choices the tile has
            int rotationChoices = 0;
            //Keep track of which rotation was chosen
            int rotationChoice = -1;
            //For each rotation option (0, 90, 180, 270)
            for(int j = 0; j < 4; j++) {
                //If this rotation is an option
                if(tileList[i].GetComponent<TileCreation>().availableRotations[j]) {
                    //Add it as a choice, and record the number
                    rotationChoices++;
                    rotationChoice = j;
                }
            }

            //If there was only one choice for both tile and rotation
            if(tileChoices == 1 && rotationChoices == 1) {
                //Create the tile
                tileList[i].GetComponent<TileCreation>().ChangeTile(tiles[tileChoice], Quaternion.Euler(new Vector3(0, rotationChoice * 90, 0)));
                //Remove the tile from the new list
                newTileList.RemoveAt(i);
            }
        }
        //Return the new list
        return newTileList;
    }
}
