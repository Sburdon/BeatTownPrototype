using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMovement : MonoBehaviour
{
    public Tilemap tilemap;
    public GameObject[] players;
    private int currentPlayerIndex = 0;
    private bool isMoveMade = false;
    private bool isCharging = false;
    private Vector3Int chargeRowStart;
    private Vector3Int chargeRowEnd;
    private Color defaultColor = Color.white;
    private Color chargeColor = Color.red;
    private bool isPlayerOneSpecialActive = false;
    private Vector3Int? playerTwoTargetPos = null;
    public int chargeDistance = 3;  // You can adjust this based on your grid size and game mechanics
    private List<Vector3Int> chargePath = new List<Vector3Int>();
    private bool isPlayerTwoCharging = false;
    public Tilemap obstacleTilemap; // Assign this in the Unity editor
    private bool endTurnMessageDisplayed = false;


    private void Start()
    {
        Debug.Log($"Player {currentPlayerIndex + 1}'s turn");
    }

    



    private void Update()
    {
        if (currentPlayerIndex == 1 && Input.GetKeyDown(KeyCode.C)) // Replace YourChargeKey with the actual key for charging
        {
            StartCharging();
        }

        if (isMoveMade && !endTurnMessageDisplayed)
        {
            Debug.Log("Player can end turn with Spacebar");
            endTurnMessageDisplayed = true;
        }


        if (Input.GetMouseButtonDown(0) && !isMoveMade)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPosition = tilemap.WorldToCell(mousePosition);

            if (CanMoveTo(gridPosition))
            {
                MoveTo(gridPosition, players[currentPlayerIndex]);
                Debug.Log($"Player {currentPlayerIndex + 1} moved");
                isMoveMade = true;
            }
        }

        if (currentPlayerIndex == 0 && Input.GetKeyDown(KeyCode.P))
        {
            isPlayerOneSpecialActive = true;
            Debug.Log("Player 1's special ability activated! Right-click a valid tile to move Player 2.");
        }

        if (isPlayerOneSpecialActive && Input.GetMouseButtonDown(1)) // Right mouse click
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPosition = tilemap.WorldToCell(mousePosition);

            if (CanMoveTo(gridPosition))
            {
                MoveOnlyPlayer2(gridPosition);
                Debug.Log("Player 1 moved Player 2 to a new position!");
                isPlayerOneSpecialActive = false;
                
            }
        }



        if (currentPlayerIndex == 1) // Player Two's special action
        {
            if (!isMoveMade && Input.GetKeyDown(KeyCode.C))
            {
                StartCharging();
                Debug.Log("Player 2 is charging!");
                isMoveMade = true;
            }
            else if (isCharging && Input.GetKeyDown(KeyCode.X))
            {
                ExecuteCharge();
                isMoveMade = true; // Ensure the turn ends after executing the charge
            }
        }

        

        if (Input.GetKeyDown(KeyCode.Space) && isMoveMade)
        {
            EndTurn();
        }
    }

    private bool CanMoveTo(Vector3Int gridPosition)
    {
        if (obstacleTilemap.HasTile(gridPosition))
        {
            return false; // Position has an obstacle
        }
        return true; // Position is free
    }

    private void MoveTo(Vector3Int gridPosition, GameObject player)
    {
        player.transform.position = tilemap.GetCellCenterWorld(gridPosition);
    }


    private void StartCharging()
    {
        isPlayerTwoCharging = true;
        Vector3Int playerPos = tilemap.WorldToCell(players[1].transform.position);
        chargeRowStart = new Vector3Int(playerPos.x, tilemap.cellBounds.yMin, playerPos.z);
        chargeRowEnd = new Vector3Int(playerPos.x - chargeDistance, playerPos.y, playerPos.z);
        UpdateChargePathTiles(playerPos);
        isCharging = true;
    }


    private void ExecuteCharge()
    {
        isPlayerTwoCharging = false;

        Vector3Int playerPos = tilemap.WorldToCell(players[currentPlayerIndex].transform.position);
        Vector3Int targetPos = playerPos;

        foreach (var tilePos in chargePath)
        {
            GameObject otherPlayer = currentPlayerIndex == 0 ? players[1] : players[0];
            Vector3Int otherPlayerPos = tilemap.WorldToCell(otherPlayer.transform.position);

            // Check for collision with the other player
            if (tilePos == otherPlayerPos)
            {
                Debug.Log("Player 2 collided with Player 1 and dealt 2 damage!");
                targetPos = new Vector3Int(otherPlayerPos.x + 1, otherPlayerPos.y, otherPlayerPos.z);
                break;
            }
            else if (obstacleTilemap.HasTile(tilePos)) // Check for obstacles
            {
                Debug.Log("Player 2's charge stopped by an obstacle!");
                targetPos = new Vector3Int(tilePos.x + 1, tilePos.y, tilePos.z); // Stop before the obstacle
                break;
            }
            targetPos = tilePos;
        }

        foreach (var tilePos in chargePath)
        {
            if (!tilemap.HasTile(tilePos))
            {
                // Skip if the tile doesn't exist in the main tilemap
                continue;
            }

            // Move Player 2 to the target position
            players[currentPlayerIndex].transform.position = tilemap.GetCellCenterWorld(targetPos);

            // Reset the colored tiles
            ResetChargeTiles();
            isCharging = false;
        }
    }


    private bool IsObstacleTile(Vector3Int tilePos)
    {
        return obstacleTilemap.HasTile(tilePos);
    }






    private void ResetChargeTiles()
    {
        for (int x = chargeRowStart.x; x <= chargeRowEnd.x; x++)
        {
            Vector3Int tilePos = new Vector3Int(x, chargeRowStart.y, chargeRowStart.z);
            tilemap.SetTileFlags(tilePos, TileFlags.None);
            tilemap.SetColor(tilePos, defaultColor);
        }
    }


    private void UpdateChargePathTiles(Vector3Int newStartPos)
    {
        // Clear previous colored tiles
        foreach (var pos in chargePath)
        {
            tilemap.SetTileFlags(pos, TileFlags.None);
            tilemap.SetColor(pos, defaultColor);
        }

        chargePath.Clear();

        for (int i = 0; i <= chargeDistance; i++)
        {
            Vector3Int newPos = new Vector3Int(newStartPos.x - i, newStartPos.y, newStartPos.z);
            if (tilemap.HasTile(newPos))
            {
                chargePath.Add(newPos);
                tilemap.SetTileFlags(newPos, TileFlags.None);
                tilemap.SetColor(newPos, chargeColor);
            }
        }
    }



    private void MoveOnlyPlayer2(Vector3Int newPosition)
    {
        players[1].transform.position = tilemap.GetCellCenterWorld(newPosition);

        // If Player Two is charging, update the charge path
        if (isPlayerTwoCharging)
        {
            UpdateChargePathTiles(newPosition);
        }
    }



    private void EndTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Length;
        isMoveMade = false;
        endTurnMessageDisplayed = false; // Reset the flag
        Debug.Log($"Player {currentPlayerIndex + 1}'s turn");
    }

}
