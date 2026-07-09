using UnityEngine; 
using ScopeRuntimeMonitoring;

public class PlayerController : MonoBehaviour
{
    [Monitor("Health", Group = "Combat", SubGroup = "Player")]
    public float health = 100f;
    [MonitorSlider("Stamina", 0f, 100f, Group = "Combat", SubGroup = "Player")]
    public float stamina = 80f;
}

public class EnemyController : MonoBehaviour
{
    [MonitorToggle("Shield Active", Group = "Combat", SubGroup = "Enemy")]
    public bool shield = true;
}