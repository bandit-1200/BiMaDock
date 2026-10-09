using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;
using System.Linq;

namespace BiMaDock
{
    public partial class MainWindow
    {
        private void DockContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            UndoCleanupMenuItem.Visibility = DockCleanup.HasBackup() ? Visibility.Visible : Visibility.Collapsed;
        }

        // Prüft alle Einträge auf nicht mehr vorhandene Ziele und lässt den Benutzer auswählen, was entfernt wird.
        private async void Cleanup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var items = SettingsManager.LoadSettings();
                IReadOnlyList<CleanupCandidate> candidates;
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                try
                {
                    candidates = await DockCleanup.FindCandidatesAsync(items);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }

                if (candidates.Count == 0)
                {
                    DialogMessageBox.Show(this, "Alles in Ordnung – es wurden keine verwaisten Einträge gefunden.", "Dock aufräumen", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new CleanupWindow(candidates) { Owner = this };
                if (dialog.ShowDialog() != true || dialog.SelectedItems.Count == 0)
                {
                    return;
                }

                DockCleanup.CreateBackup(items);
                SettingsManager.SaveSettings(DockCleanup.RemoveItems(items, dialog.SelectedItems));
                ReloadDockAfterCleanup();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup_Click Fehler: {ex}");
                DialogMessageBox.Show(this, "Beim Aufräumen ist ein Fehler aufgetreten:" + Environment.NewLine + ex.Message, "Dock aufräumen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Stellt die beim letzten Aufräumen entfernten Einträge wieder her. Seitdem hinzugefügte oder
        // geänderte Einträge bleiben erhalten; nur fehlende Einträge aus der Sicherung kommen zurück.
        private void UndoCleanup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backup = DockCleanup.LoadBackup();
                if (backup == null)
                {
                    DialogMessageBox.Show(this, "Es ist keine Sicherung zum Wiederherstellen vorhanden.", "Aufräumen rückgängig machen", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var current = SettingsManager.LoadSettings();
                var currentIds = new HashSet<string>(current.Select(item => item.Id));
                var restored = backup.Where(item => !currentIds.Contains(item.Id)).ToList();

                var merged = new List<DockItem>(current);
                merged.AddRange(restored);
                SettingsManager.SaveSettings(merged);
                DockCleanup.DeleteBackup();
                ReloadDockAfterCleanup();

                DialogMessageBox.Show(this, restored.Count == 1 ? "1 Eintrag wurde wiederhergestellt." : $"{restored.Count} Einträge wurden wiederhergestellt.", "Aufräumen rückgängig machen", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UndoCleanup_Click Fehler: {ex}");
                DialogMessageBox.Show(this, "Beim Wiederherstellen ist ein Fehler aufgetreten:" + Environment.NewLine + ex.Message, "Aufräumen rückgängig machen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReloadDockAfterCleanup()
        {
            HideCategoryDockPanel();
            dockManager.LoadDockItems();
            RefreshDockNavigation();
        }

        // Methode zum Öffnen des Einstellungsfensters
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow(this);
            settingsWindow.ShowDialog(); // Modal anzeigen
        }



        private void Open_Click(object sender, RoutedEventArgs e)
        {
            dockManager.Open_Click(sender, e);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var customMessageBox = new CustomMessageBox("Möchtest du BiMaDock wirklich beenden?")
                {
                    Owner = this
                };

                customMessageBox.ShowDialog();

                if (customMessageBox.Result)
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exit_Click Fehler: {ex}");
                DialogMessageBox.Show(this, "Beim Beenden von BiMaDock ist ein Fehler aufgetreten.", "Beenden", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
            {
                var customMessageBox = new CustomMessageBox($"Möchtest du das Element '{dockItem.DisplayName}' wirklich löschen?")
                {
                    Owner = this
                };
                customMessageBox.ShowDialog();

                if (customMessageBox.Result)
                {
                    // Hier ermitteln wir die Kategorie, die gelöscht werden soll
                    string currentCategory = dockItem.Category;

                    // Übergabe der Kategorie an RemoveDockItem
                    dockManager.RemoveDockItem(button, currentCategory);

                    HideCategoryDockPanel();

                }
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputDialog = new InputDialog("Kategorie erstellen", "Bitte geben Sie den Namen der Kategorie ein:")
                {
                    Owner = this
                };

                if (inputDialog.ShowDialog() == true)
                {
                    string categoryName = inputDialog.Answer?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(categoryName))
                    {
                        DialogMessageBox.Show(this, "Bitte geben Sie einen gültigen Kategorienamen ein.", "Kategorie erstellen", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var existingItems = SettingsManager.LoadSettings();
                    bool alreadyExists = existingItems.Any(item =>
                        item.IsCategory &&
                        string.Equals(item.DisplayName, categoryName, StringComparison.OrdinalIgnoreCase));

                    if (alreadyExists)
                    {
                        DialogMessageBox.Show(this, $"Die Kategorie \"{categoryName}\" existiert bereits.", "Kategorie erstellen", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    dockManager.AddCategoryItem(categoryName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddCategory_Click Fehler: {ex}");
                DialogMessageBox.Show(this, $"Beim Erstellen der Kategorie ist ein Fehler aufgetreten:\n{ex.Message}", "Kategorie erstellen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            currentDockStatus |= DockStatus.EditSettingsDock;  // Flag setzen
            try
            {
                if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
                {

                    if (dockItem.IsCategory)
                    {
                        HideCategoryDockPanel();
                    }

                    // Alle Dock-Items laden
                    var dockItems = SettingsManager.LoadSettings() ?? new List<DockItem>();

                    // Prüfen, ob das aktuelle Item existiert
                    var settings = dockItems.FirstOrDefault(di => di.Id == dockItem.Id);

                    if (settings == null)
                    {
                        DialogMessageBox.Show("Fehler beim Laden der Dock-Einstellungen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }


                    // Edit-Dialog initialisieren
                    EditPropertiesWindow editWindow = new EditPropertiesWindow
                    {
                        Owner = this,
                        IdTextBox = { Text = settings.Id },
                        NameTextBox = { Text = settings.DisplayName },
                        IconSourceTextBox = { Text = settings.IconSource }, // Hinzufügen des Bildpfads
                        CategoryTextBox = { Text = settings.Category },
                        IsCategoryTextBox = { Text = settings.IsCategory.ToString() },
                        ApplicationPathTextBox = { Text = settings.FilePath.ToString() },

                        DockItem = settings
                    };

                    bool? dialogResult = editWindow.ShowDialog();
                    if (dialogResult == true)
                    {
                        string newName = editWindow.NameTextBox.Text.Trim();
                        string newIconPath = editWindow.IconSourceTextBox.Text.Trim(); // Neues Bildpfad

                        // Neuen Namen validieren
                        if (string.IsNullOrEmpty(newName))
                        {
                            DialogMessageBox.Show("Name darf nicht leer sein.", "Ungültiger Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Prüfen, ob der Name geändert wurde
                        bool nameChanged = settings.DisplayName != newName;

                        if (nameChanged)
                        {
                            settings.DisplayName = newName;

                            // Aktualisiere den Button-Text
                            var textBlock = new TextBlock
                            {
                                Text = newName,
                                TextAlignment = TextAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                Width = 60,
                                Margin = new Thickness(5)
                            };

                            var stackPanel = button.Content as StackPanel;
                            if (stackPanel != null)
                            {
                                stackPanel.Children.RemoveAt(1);
                                stackPanel.Children.Add(textBlock);
                            }
                        }

                        // Wenn der Name nicht geändert wurde oder das Symbol aktualisiert werden muss
                        if (!string.IsNullOrEmpty(newIconPath))
                        {
                            settings.IconSource = newIconPath; // Hier den Bildpfad aktualisieren
                        }

                        // Speichern der aktualisierten Einstellungen
                        SettingsManager.SaveSettings(dockItems);
                        IconHelper.ClearCache(); // Symbol kann sich geändert haben

                        if (!string.IsNullOrEmpty(dockItem.Category))
                        {
                            HideCategoryDockPanel();
                            StackPanel categoryDock = new StackPanel
                            {
                                Tag = dockItem.Category
                            };
                            ShowCategoryDockPanel(categoryDock);
                        }
                        else
                        {
                            dockManager.LoadDockItems();
                        }


                    }
                }
                else
                {
                    DialogMessageBox.Show("Fehler: DockContextMenu.PlacementTarget ist kein Button oder button.Tag ist kein DockItem", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DialogMessageBox.Show($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckAutostart()
        {
            AutostartCheckBox.IsChecked = StartupManager.IsInStartup();
        }

        // Das Umschalten löst AutostartCheckBox_Checked/_Unchecked aus, die den Registry-Eintrag schreiben.
        private void AutostartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AutostartCheckBox.IsChecked = AutostartCheckBox.IsChecked != true;
        }



        private void AutostartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            StartupManager.AddToStartup(true);
        }

        private void AutostartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupManager.AddToStartup(false);
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AboutWindow aboutWindow = new AboutWindow
                {
                    Owner = this
                };
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AboutMenuItem_Click Fehler: {ex}");
                DialogMessageBox.Show(this, $"Beim Öffnen des Info-Fensters ist ein Fehler aufgetreten:\n{ex.Message}", "Über BiMaDock", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpdateCheck()
        {
            if (Environment.GetEnvironmentVariable("BIMADOCK_DISABLE_UPDATE_CHECK") == "1")
            {
                return;
            }

            await UpdateChecker.CheckForUpdatesAsync();

        }


        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://bandit-1200.github.io/BiMaDock";
            try
            {
                using var process = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DialogMessageBox.Show($"Fehler beim Öffnen der URL: {ex.Message}");
            }
        }

        private void OpenFilePath_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
            {
                string filePath = dockItem.FilePath;
                if (!string.IsNullOrEmpty(filePath))
                {
                    // Startet den Explorer und zeigt die Datei an
                    var startInfo = new ProcessStartInfo("explorer.exe");
                    startInfo.ArgumentList.Add($"/select,{filePath}");
                    using var process = Process.Start(startInfo);
                }
            }
        }
    }
}
