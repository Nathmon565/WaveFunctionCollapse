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
                dungeonTiles.Add(Instantiate(defaultTile, new Vector3(x * 10, 0, y * 10), Quaternion.Euler(Vector3.zero), dungeonRoot.transform));
                //dungeonTiles.Add(Instantiate(tiles[Random.Range(0, tiles.ToArray().Length)], new Vector3(x * 10, 0, y * 10), Quaternion.Euler(Vector3.zero), dungeonRoot.transform));
            }
        }


        Debug.Log("Dungeon generation complete! Total time taken: " + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100000) / 100f) + "ms (" + (Mathf.Round((Time.realtimeSinceStartup - timeStart) * 100) / 100f) + " seconds)");
    }

    ///<summary>Create an instantiated and prepared version of the tile to be duplicated</summary>
    ///<param name="tile">The tile to be optimized</param>
    ///<param name="posModifier">The x coordinate modifier to esnure the raycast is not blocked</param>
    public GameObject OptimizeTile(GameObject tile, Transform parent, float posModifier) {
        GameObject newTile = Instantiate(tile, new Vector3(posModifier, 0, 0), Quaternion.Euler(Vector3.zero), parent);
        StartCoroutine(WaitUntilReady(newTile.GetComponent<DungeonTile>()));
        return newTile;
    }

    ///<summary>Waits until the tile's readyToUse boolean is true</summary>
    ///<param name="tile">The tile to check</param>
    private IEnumerator WaitUntilReady(DungeonTile tile) {
        yield return new WaitUntil(() => tile.readyToUse);
    }
}
