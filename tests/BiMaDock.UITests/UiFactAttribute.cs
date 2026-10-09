using Xunit;

namespace BiMaDock.UITests;

/// <summary>
/// UI-Tests übernehmen Maus und Tastatur und laufen deshalb nur auf ausdrücklichen Wunsch:
/// Umgebungsvariable BIMADOCK_UI_TESTS=1 setzen (z. B. über ui-test.ps1).
/// </summary>
public sealed class UiFactAttribute : FactAttribute
{
    public const string EnableVariable = "BIMADOCK_UI_TESTS";

    public UiFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(EnableVariable) != "1")
        {
            Skip = $"UI-Tests sind deaktiviert. Zum Ausführen {EnableVariable}=1 setzen.";
        }
    }
}
