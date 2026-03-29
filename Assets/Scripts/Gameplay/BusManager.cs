using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusManager : MonoBehaviour
{
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private Transform busStopPosition;
    [SerializeField] private Transform busSpawnPoint;
    [SerializeField] private Transform busDespawnPoint;
    [SerializeField] private float busDriveSpeed = 8f;
    [SerializeField] private ColorConfig colorConfig;

    private BusDefinition[] busSequence;
    private int currentBusIndex;
    private GameObject currentBus;
    private int currentBusRemainingCapacity;
    private bool isProcessing;

    public event Action OnAllBusesComplete;
    public event Action<StickmanColor> OnBusReady;

    public StickmanColor CurrentBusColor => busSequence[currentBusIndex].color;

    public void Initialize(BusDefinition[] sequence)
    {
        busSequence = sequence;
        currentBusIndex = 0;
        isProcessing = false;
    }

    public void SpawnNextBus()
    {
        if (currentBusIndex >= busSequence.Length)
        {
            OnAllBusesComplete?.Invoke();
            return;
        }

        StartCoroutine(DriveIn());
    }

    private IEnumerator DriveIn()
    {
        isProcessing = true;
        var def = busSequence[currentBusIndex];
        currentBusRemainingCapacity = def.capacity;

        Vector3 spawnPos = busSpawnPoint != null ? busSpawnPoint.position : transform.position + Vector3.right * 15f;
        Vector3 stopPos = busStopPosition != null ? busStopPosition.position : transform.position;

        currentBus = ObjectPool.Instance.Get(busPrefab, spawnPos, Quaternion.identity);

        var busVisual = currentBus.GetComponent<BusVisual>();
        if (busVisual != null)
            busVisual.SetColor(colorConfig.GetRenderColor(def.color));

        while (Vector3.Distance(currentBus.transform.position, stopPos) > 0.1f)
        {
            currentBus.transform.position = Vector3.MoveTowards(
                currentBus.transform.position, stopPos, busDriveSpeed * Time.deltaTime);
            yield return null;
        }

        currentBus.transform.position = stopPos;
        isProcessing = false;
        OnBusReady?.Invoke(def.color);
    }

    public void TryLoadPassengers(BusStop busStop)
    {
        if (isProcessing || currentBus == null) return;
        if (currentBusIndex >= busSequence.Length) return;

        var matching = busStop.RemoveMatchingPassengers(busSequence[currentBusIndex].color);

        if (matching.Count == 0) return;

        StartCoroutine(LoadAndCheckDepart(matching, busStop));
    }

    private IEnumerator LoadAndCheckDepart(List<Stickman> passengers, BusStop busStop)
    {
        isProcessing = true;

        int boarded = 0;
        Vector3 busPos = currentBus.transform.position;

        for (int i = 0; i < passengers.Count; i++)
        {
            if (currentBusRemainingCapacity <= 0) break;

            var passenger = passengers[i];
            bool done = false;
            passenger.BoardBus(busPos, () => done = true);

            while (!done) yield return null;

            passenger.gameObject.SetActive(false);
            currentBusRemainingCapacity--;
            boarded++;
        }

        if (currentBusRemainingCapacity <= 0)
        {
            yield return StartCoroutine(DriveOut());
            currentBusIndex++;
            SpawnNextBus();
        }
        else
        {
            isProcessing = false;
        }
    }

    private IEnumerator DriveOut()
    {
        Vector3 despawnPos = busDespawnPoint != null ? busDespawnPoint.position : transform.position - Vector3.right * 15f;

        while (Vector3.Distance(currentBus.transform.position, despawnPos) > 0.1f)
        {
            currentBus.transform.position = Vector3.MoveTowards(
                currentBus.transform.position, despawnPos, busDriveSpeed * Time.deltaTime);
            yield return null;
        }

        ObjectPool.Instance.Return(currentBus);
        currentBus = null;
    }

    public bool IsProcessing => isProcessing;
}
