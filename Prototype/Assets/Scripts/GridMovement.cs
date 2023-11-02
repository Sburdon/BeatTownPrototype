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

        if (currentPlayerIndex == 0 && Input.GetKeyDown(KeyCode.P) && !isMoveMade)
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
                isMoveMade = true;
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

        if (isMoveMade)
        {
            Debug.Log("Player can end turn with Spacebar");
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

        // Get the position of Player One
        Vector3Int playerOnePos = tilemap.WorldToCell(players[0].transform.position);
        Vector3Int targetPos = chargePath[chargePath.Count - 1]; // Default to the last tile of the charge path

        foreach (var tilePos in chargePath)
        {
            if (tilePos == playerOnePos)
            {
                Debug.Log("Player 2 collided with Player 1 and dealt 2 damage!");

                // Assuming the charge path goes from right to left,
                // place Player Two to the right of Player One
                targetPos = new Vector3Int(playerOnePos.x + 1, playerOnePos.y, playerOnePos.z);
                break;
            }
        }

        // Move Player Two to the target position
        players[1].transform.position = tilemap.GetCellCenterWorld(targetPos);

        // Reset the colored tiles
        ResetChargeTiles();

        isCharging = false;
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
        Debug.Log($"Player {currentPlayerIndex + 1}'s turn");
    }
}
