using UnityEngine;
using UnityEngine.InputSystem;

public class ArrowRain : MonoBehaviour
{
    public GameObject arrowPrefab;
    public float spawnHeight = 15f;
    public float rectangleWidth = 20f;
    public float rectangleDepth = 10f;
    public int arrowsPerRow = 10;
    public int rowCount = 5;
    public float spawnDelay = 0.05f;
    public float distanceFromPlayer = 5f;

    private bool isRaining;
    private int currentArrowIndex;
    private float nextSpawnTime;
    private int totalArrows;

    void Update()
    {
        if (isRaining && Time.time >= nextSpawnTime && currentArrowIndex < totalArrows)
        {
            SpawnArrow();
            currentArrowIndex++;
            nextSpawnTime = Time.time + spawnDelay;

            if (currentArrowIndex >= totalArrows)
            {
                StopRain();
            }
        }
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartRain();
        }
    }

    void SpawnArrow()
    {
        int row = currentArrowIndex / arrowsPerRow;
        int col = currentArrowIndex % arrowsPerRow;

        float stepWidth = rectangleWidth / (arrowsPerRow - 1);
        float stepDepth = rectangleDepth / (rowCount - 1);
        float offsetX = (col * stepWidth) - (rectangleWidth / 2f);
        float offsetZ = (row * stepDepth);

        Vector3 rectangleStart = transform.position + transform.forward * distanceFromPlayer;
        Vector3 spawnPosition = rectangleStart + transform.right * offsetX + transform.forward * offsetZ + Vector3.up * spawnHeight;

        Quaternion spawnRotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        Instantiate(arrowPrefab, spawnPosition, spawnRotation);
    }

    public void StartRain()
    {
        isRaining = true;
        currentArrowIndex = 0;
        totalArrows = arrowsPerRow * rowCount;
        nextSpawnTime = Time.time;
        Debug.Log("Pluie de flèches démarrée ! Total: " + totalArrows);
    }

    public void StopRain()
    {
        isRaining = false;
        Debug.Log("Pluie de flèches terminée !");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 rectangleStart = transform.position + transform.forward * distanceFromPlayer;
        Vector3 frontLeft = rectangleStart - transform.right * (rectangleWidth / 2f);
        Vector3 frontRight = rectangleStart + transform.right * (rectangleWidth / 2f);
        Vector3 backLeft = frontLeft + transform.forward * rectangleDepth;
        Vector3 backRight = frontRight + transform.forward * rectangleDepth;

        Gizmos.DrawLine(frontLeft, frontRight);
        Gizmos.DrawLine(frontRight, backRight);
        Gizmos.DrawLine(backRight, backLeft);
        Gizmos.DrawLine(backLeft, frontLeft);

        Vector3 heightOffset = Vector3.up * spawnHeight;
        Gizmos.DrawLine(frontLeft + heightOffset, frontRight + heightOffset);
        Gizmos.DrawLine(frontRight + heightOffset, backRight + heightOffset);
        Gizmos.DrawLine(backRight + heightOffset, backLeft + heightOffset);
        Gizmos.DrawLine(backLeft + heightOffset, frontLeft + heightOffset);

        Gizmos.DrawLine(frontLeft, frontLeft + heightOffset);
        Gizmos.DrawLine(frontRight, frontRight + heightOffset);
        Gizmos.DrawLine(backLeft, backLeft + heightOffset);
        Gizmos.DrawLine(backRight, backRight + heightOffset);
    }
}
