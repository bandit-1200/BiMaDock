using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace BiMaDock
{
    /// <summary>
    /// Typisiertes Abbild von StyleSettings.json (Format wie von SettingsWindow.SaveButton_Click geschrieben).
    /// Alle Werte sind optional, damit fehlende Einträge die bisherigen Standardwerte nicht überschreiben.
    /// </summary>
    internal sealed class StyleSettingsFile
    {
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? AccentColor { get; set; }
        public string? FeedbackColor { get; set; }
        public int? DockShowDelayMilliseconds { get; set; }
        public bool? CategoryFlowAnimationEnabled { get; set; }
        public int? CategoryFlowAnimationMilliseconds { get; set; }
        public int? SelectedEffectIndex { get; set; }
        public ScaleSection? Scale { get; set; }
        public RotateSection? Rotate { get; set; }
        public TranslateSection? Translate { get; set; }
        public SwingSection? Swing { get; set; }

        public sealed class ScaleSection
        {
            public double? Duration { get; set; }
            public double? ScaleFactor { get; set; }
            public bool? AutoReverse { get; set; }
            public int? EffectIndex { get; set; }
        }

        public sealed class RotateSection
        {
            public double? Duration { get; set; }
            public double? Angle { get; set; }
            public bool? AutoReverse { get; set; }
            public int? EffectIndex { get; set; }
        }

        public sealed class TranslateSection
        {
            public double? Duration { get; set; }
            public double? TranslateX { get; set; }
            public double? TranslateY { get; set; }
            public bool? AutoReverse { get; set; }
            public int? EffectIndex { get; set; }
        }

        public sealed class SwingSection
        {
            public double? Duration { get; set; }
            public double? Angle { get; set; }
        }

        /// <summary>
        /// Liest StyleSettings.json einmal ein. Gibt null zurück, wenn die Datei fehlt oder ungültig ist.
        /// </summary>
        public static StyleSettingsFile? Load(string? path = null)
        {
            path ??= AppPaths.GetSettingsFilePath("StyleSettings.json");
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<StyleSettingsFile>(File.ReadAllText(path));
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"StyleSettings.json ist ungültig: {ex.Message}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"StyleSettings.json konnte nicht gelesen werden: {ex.Message}");
            }
            catch (System.UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Kein Zugriff auf StyleSettings.json: {ex.Message}");
            }

            return null;
        }
    }
}
