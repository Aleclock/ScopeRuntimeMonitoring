using UnityEngine;
using ScopeRuntimeMonitoring;

public class MonitoredExample : MonoBehaviour
{
    public float currentHealth = 100f;

    [Monitor("Health")]
    public float Health => currentHealth;

    [Monitor("Is Alive")]
    public bool IsAlive => currentHealth > 0;

    [Monitor("Score")]
    public int Score => (int)(currentHealth * 2);

    [Monitor("Position")]
    public Vector3 Position => transform.position;

    private void OnEnable() => Monitor.StartMonitoring(this);
    private void OnDisable() => Monitor.StopMonitoring(this);

    private void Update()
    {
        currentHealth = Mathf.PingPong(Time.time * 10f, 100f);
    }
}