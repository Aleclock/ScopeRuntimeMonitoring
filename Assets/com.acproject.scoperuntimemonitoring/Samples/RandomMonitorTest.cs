using UnityEngine;

public class RandomMonitorTest : MonoBehaviour
{
    [SerializeField] private float stamina = 75f;
    [SerializeField] private int coins = 12;
    [SerializeField] private bool shielded = true;
    [SerializeField] private string statusText = "Booting";
    [SerializeField] private Vector3 velocity = new Vector3(1f, 0f, 0f);

    [MonitorSlider("Stamina", 0f, 100f, 0.5f)]
    public float Stamina => stamina;

    [MonitorValue("Coins")]
    public int Coins => coins;

    [MonitorToggle("Shielded")]
    public bool Shielded => shielded;

    [MonitorInputValue("Status")]
    public string StatusText => statusText;

    [MonitorProgress("Velocity", 0f, 20f)]
    public Vector3 Velocity => velocity;

    private void OnEnable()
    {
        Monitor.StartMonitoring(this);
    }

    private void OnDisable()
    {
        Monitor.StopMonitoring(this);
    }

    private void Update()
    {
        stamina = Mathf.PingPong(Time.time * 18f, 100f);
        coins = Mathf.FloorToInt(Mathf.PingPong(Time.time * 3f, 99f));
        shielded = Mathf.Sin(Time.time * 0.75f) > 0f;
        statusText = shielded ? "Shield online" : "Shield offline";
        velocity = new Vector3(
            Mathf.Sin(Time.time * 1.2f) * 10f,
            Mathf.Cos(Time.time * 0.8f) * 6f,
            Mathf.Sin(Time.time * 0.5f) * 4f);
    }
}