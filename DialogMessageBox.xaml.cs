using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BiMaDock
{
    /// <summary>
    /// Gestalteter Ersatz für <see cref="MessageBox"/> im dunklen Dialogstil der App.
    /// Fällt auf die System-MessageBox zurück, wenn der gestaltete Dialog nicht angezeigt werden kann.
    /// </summary>
    public partial class DialogMessageBox : Window
    {
        private MessageBoxResult result;
        private bool wasShown;

        private DialogMessageBox(string message, string caption, MessageBoxButton buttons, MessageBoxImage image)
        {
            InitializeComponent();

            Title = caption;
            CaptionTextBlock.Text = caption;
            MessageTextBlock.Text = message;
            ApplyIcon(image);
            result = GetCancelResult(buttons);
            CreateButtons(buttons);
            Loaded += (_, _) => wasShown = true;
        }

        /// <summary>Zeigt einen Meldungsdialog ohne Besitzerfenster an.</summary>
        public static MessageBoxResult Show(string message, string caption = "BiMaDock",
            MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return Show(null, message, caption, buttons, image);
        }

        /// <summary>Zeigt einen Meldungsdialog an, zentriert über <paramref name="owner"/>, falls dieser sichtbar ist.</summary>
        public static MessageBoxResult Show(Window? owner, string message, string caption,
            MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            message ??= string.Empty;
            caption ??= string.Empty;

            var application = Application.Current;
            if (application == null)
            {
                return ShowFallback(owner, message, caption, buttons, image);
            }

            var dispatcher = application.Dispatcher;
            if (!dispatcher.CheckAccess())
            {
                if (dispatcher.HasShutdownStarted)
                {
                    return ShowFallback(null, message, caption, buttons, image);
                }

                try
                {
                    return dispatcher.Invoke(() => Show(owner, message, caption, buttons, image));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DialogMessageBox: Marshalling auf den UI-Thread fehlgeschlagen: {ex}");
                    return ShowFallback(null, message, caption, buttons, image);
                }
            }

            DialogMessageBox? dialog = null;
            try
            {
                // Ohne geladene Stile (z. B. sehr früh beim Start) wäre der Dialog unsichtbar bzw. ungestaltet
                if (application.TryFindResource("DialogWindowStyle") == null)
                {
                    return ShowFallback(owner, message, caption, buttons, image);
                }

                dialog = new DialogMessageBox(message, caption, buttons, image);

                if (owner != null && owner != dialog && owner.CheckAccess() && owner.IsLoaded && owner.IsVisible)
                {
                    dialog.Owner = owner;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                dialog.ShowDialog();
                return dialog.result;
            }
            catch (Exception ex) when (dialog == null || !dialog.wasShown)
            {
                // Nur Fehler beim Erstellen/Öffnen abfangen; spätere Ausnahmen aus der modalen Schleife weiterreichen
                Debug.WriteLine($"DialogMessageBox: Gestalteter Dialog fehlgeschlagen: {ex}");
                try
                {
                    dialog?.Close();
                }
                catch
                {
                    // Fenster war nicht geöffnet
                }

                return ShowFallback(owner, message, caption, buttons, image);
            }
        }

        private static MessageBoxResult ShowFallback(Window? owner, string message, string caption,
            MessageBoxButton buttons, MessageBoxImage image)
        {
            if (owner != null && owner.CheckAccess())
            {
                try
                {
                    return MessageBox.Show(owner, message, caption, buttons, image);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DialogMessageBox: MessageBox mit Besitzer fehlgeschlagen: {ex}");
                }
            }

            return MessageBox.Show(message, caption, buttons, image);
        }

        private static MessageBoxResult GetCancelResult(MessageBoxButton buttons) => buttons switch
        {
            MessageBoxButton.OKCancel => MessageBoxResult.Cancel,
            MessageBoxButton.YesNoCancel => MessageBoxResult.Cancel,
            MessageBoxButton.YesNo => MessageBoxResult.No,
            _ => MessageBoxResult.OK
        };

        private void CreateButtons(MessageBoxButton buttons)
        {
            MessageBoxResult cancelResult = GetCancelResult(buttons);
            var definitions = buttons switch
            {
                MessageBoxButton.OKCancel => new[] { ("OK", MessageBoxResult.OK), ("Abbrechen", MessageBoxResult.Cancel) },
                MessageBoxButton.YesNo => new[] { ("Ja", MessageBoxResult.Yes), ("Nein", MessageBoxResult.No) },
                MessageBoxButton.YesNoCancel => new[] { ("Ja", MessageBoxResult.Yes), ("Nein", MessageBoxResult.No), ("Abbrechen", MessageBoxResult.Cancel) },
                _ => new[] { ("OK", MessageBoxResult.OK) }
            };

            Button? defaultButton = null;
            for (int i = 0; i < definitions.Length; i++)
            {
                var (text, buttonResult) = definitions[i];
                bool isPrimary = i == 0;
                var button = new Button
                {
                    Content = text,
                    MinWidth = 110,
                    IsDefault = isPrimary,
                    IsCancel = buttonResult == cancelResult
                };
                button.SetResourceReference(StyleProperty, isPrimary ? "DialogPrimaryButtonStyle" : "DialogSecondaryButtonStyle");
                button.Click += (_, _) => CloseWithResult(buttonResult);
                ButtonPanel.Children.Add(button);
                defaultButton ??= button;
            }

            Loaded += (_, _) => defaultButton?.Focus();
        }

        private void CloseWithResult(MessageBoxResult buttonResult)
        {
            result = buttonResult;
            try
            {
                DialogResult = buttonResult is MessageBoxResult.OK or MessageBoxResult.Yes;
            }
            catch (InvalidOperationException)
            {
                // Nicht modal angezeigt
                Close();
            }
        }

        private void ApplyIcon(MessageBoxImage image)
        {
            // MessageBoxImage hat doppelte Werte (Error = Hand = Stop, Warning = Exclamation, Information = Asterisk)
            (string glyph, Color color)? icon = image switch
            {
                MessageBoxImage.Error => ("", Color.FromRgb(0xE5, 0x53, 0x4B)),
                MessageBoxImage.Warning => ("", Color.FromRgb(0xE0, 0xB2, 0x52)),
                MessageBoxImage.Information => ("", Color.FromRgb(0x4C, 0xA0, 0xF0)),
                MessageBoxImage.Question => ("", Color.FromRgb(0x4C, 0xA0, 0xF0)),
                _ => null
            };

            if (icon == null)
            {
                IconGlyph.Visibility = Visibility.Collapsed;
                return;
            }

            IconGlyph.Text = icon.Value.glyph;
            IconGlyph.Foreground = new SolidColorBrush(icon.Value.color);
            IconGlyph.Visibility = Visibility.Visible;
        }
    }
}
