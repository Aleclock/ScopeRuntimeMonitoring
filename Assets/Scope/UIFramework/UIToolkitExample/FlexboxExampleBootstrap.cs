using UnityEngine;
using UnityEngine.UIElements;

public sealed class FlexboxExampleBootstrap : MonoBehaviour
{
    private UIDocument _document;
    private bool _built;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        TryBuildExample();
    }

    private void Start()
    {
        TryBuildExample();
    }

    private void TryBuildExample()
    {
        if (_built)
            return;

        if (_document == null)
            _document = GetComponent<UIDocument>();

        if (_document == null || _document.rootVisualElement == null)
            return;

        var root = _document.rootVisualElement;
        root.Clear();

        root.style.flexGrow = 1;
        root.style.width = Length.Percent(100);
        root.style.height = Length.Percent(100);

        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.flexWrap = Wrap.Wrap;
        container.style.alignItems = Align.FlexStart;
        container.style.width = Length.Percent(100);
        container.style.height = Length.Percent(100);
        container.style.paddingLeft = 12;
        container.style.paddingRight = 12;
        container.style.paddingTop = 12;
        container.style.paddingBottom = 12;

        for (var i = 1; i <= 6; i++)
        {
            container.Add(CreatePanel(i));
        }

        root.Add(container);

        _built = true;
    }

    private VisualElement CreatePanel(int index)
    {
        var panel = new VisualElement();
        panel.style.backgroundColor = new Color(0.17f, 0.19f, 0.22f, 0.8f);
        panel.style.color = Color.white;
        panel.style.paddingLeft = 8;
        panel.style.paddingRight = 8;
        panel.style.paddingTop = 8;
        panel.style.paddingBottom = 8;
        panel.style.marginRight = 12;
        panel.style.marginBottom = 12;
        panel.style.width = 240;
        panel.style.height = 190;
        panel.style.minWidth = 220;
        panel.style.maxWidth = 420;
        panel.style.flexShrink = 0;
        panel.style.flexGrow = 0;
        panel.style.alignSelf = Align.FlexStart;

        var title = new Label($"Gameplay{index}");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 6;
        title.style.fontSize = 18;

        panel.Add(title);
        panel.Add(new Label("Health: 48.47"));
        panel.Add(new Label("IsAlive: true"));
        panel.Add(new Label("Position: (0.00, 0.00, 0.00)"));
        panel.Add(new Label("Score: 96"));

        return panel;
    }
}