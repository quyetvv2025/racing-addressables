using System.Collections.Generic;
using UnityEngine;

public class TrafficPool : MonoBehaviour
{
    public TrafficCar prefab;
    public int preload = 20;

    readonly Queue<TrafficCar> pool = new Queue<TrafficCar>();

    void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError("TrafficPool: prefab not assigned.");
            enabled = false;
            return;
        }

        for (int i = 0; i < preload; i++)
        {
            var car = Instantiate(prefab, transform);
            car.gameObject.SetActive(false);
            pool.Enqueue(car);
        }
    }

    public TrafficCar Get()
    {
        TrafficCar car = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, transform);
        car.gameObject.SetActive(true);
        return car;
    }

    public void Return(TrafficCar car)
    {
        car.gameObject.SetActive(false);
        pool.Enqueue(car);
    }
}
