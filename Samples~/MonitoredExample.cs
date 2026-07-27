using UnityEngine;
using ScopeRuntimeMonitoring;

public class MonitoredExample : MonoBehaviour, IMonitorGroupCustomizer
{
    public float currentHealth = 100f;

    [Monitor("Health", Group = "Combat", SubGroup = "Player", Order = 1, SubGroupOrder = 1)]
    public float Health => currentHealth;

    [Monitor("Is Alive", Group = "Combat", SubGroup = "Player", Order = 0, SubGroupOrder = 1)]
    public bool IsAlive => currentHealth > 0;

    [Monitor("Score", Group = "Combat", SubGroup = "Global", SubGroupOrder = 0)]
    public int Score => (int)(currentHealth * 2);

    [Monitor("Position")]
    public Vector3 Position => transform.position;

    [Monitor("Active Quests", Group = "RPG Log")]
    public System.Collections.Generic.List<string> Quests => new System.Collections.Generic.List<string>
    {
        "Defeat the dragon",
        "Collect 5 herbs",
        "Talk to the villager"
    };

    public string GetMonitorGroup() => $"Combat - {gameObject.name}";

    private void OnEnable() => Monitor.StartMonitoring(this);
    private void OnDisable() => Monitor.StopMonitoring(this);

    private void Update()
    {
        currentHealth = Mathf.PingPong(Time.time * 10f, 100f);
    }
}