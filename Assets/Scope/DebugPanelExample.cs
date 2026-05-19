using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleUsage : MonoBehaviour
{
    private void Start()
    {
        // Create an instance of FPSDebugPanel through DebugManager
        var fpsPanel = DebugManager.Instance.CreateDebugPanel<FPSDebugPanel>();
        fpsPanel.Initialize();
    }
}
