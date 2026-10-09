namespace GamePause.SalahEvents.PrayerTimeAccuracy
{
    using System;

    /// <summary>
    /// Renders a button next to a public void method in the Unity Inspector.
    /// (Unity 6000.2.10f1 does not ship UnityEngine.Button, so the attribute +
    /// drawer are provided locally; usage is identical: [Button("label")].)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : UnityEngine.PropertyAttribute
    {
        public readonly string text;
        public ButtonAttribute() : this("Invoke") { }
        public ButtonAttribute(string text) { this.text = text; }
    }
}
