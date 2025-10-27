using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Spreadsheet.Core;
using Cell = Spreadsheet.Core.Cell;

#if WINDOWS
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
#endif

namespace oop_lab1_task2
{
    public partial class MainPage : ContentPage, ICellValueProvider
    {
        private List<List<Cell>> cellData = new List<List<Cell>>();
        private Dictionary<string, Cell> cellMap = new Dictionary<string, Cell>(StringComparer.OrdinalIgnoreCase);
        private int currentRowCount = 0;
        private int currentColumnCount = 20;
        private HashSet<string> currentlyCalculating = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool showExpression = true;
        private Entry? currentSelectedEntry = null;
        private string currentSelectedCellName = "";
        

        public MainPage()
        {
            InitializeComponent();
            CreateGrid();
        }

        private void CreateGrid()
        {
            cellData.Clear();
            cellMap.Clear();
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();
            currentRowCount = 50;
            currentColumnCount = 20;
            AddColumnsAndColumnLabels();
            AddRowsAndCellEntries();
            textInput.Text = ""; 
        }

        private void AddColumnsAndColumnLabels()
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int col = 0; col < currentColumnCount; col++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var label = new Label
                {
                    Text = GetColumnName(col + 1),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(5)
                };
                Grid.SetRow(label, 0);
                Grid.SetColumn(label, col + 1);
                grid.Children.Add(label);
            }
        }

        private void AddRowsAndCellEntries()
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int row = 0; row < currentRowCount; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var rowData = new List<Cell>();
                var label = new Label
                {
                    Text = (row + 1).ToString(),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(5)
                };
                Grid.SetRow(label, row + 1);
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                for (int col = 0; col < currentColumnCount; col++)
                {
                    var cellName = GetCellName(row, col);
                    var cell = new Cell { Name = cellName, Value = double.NaN };
                    rowData.Add(cell);
                    cellMap[cellName] = cell;
                    var entry = new Entry
                    {
                        Text = "",
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Fill,
                        StyleId = cellName
                    };
                    
                    entry.Focused += Entry_Focused;
                    entry.Completed += Cell_ContentChanged;
                    entry.Unfocused += Cell_ContentChanged;

                    Grid.SetRow(entry, row + 1);
                    Grid.SetColumn(entry, col + 1);
                    grid.Children.Add(entry);
                }

                cellData.Add(rowData);
            }
        }

        private string GetColumnName(int colIndex)
        {
            int dividend = colIndex;
            string columnName = string.Empty;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo - 1) / 26;
            }

            return columnName;
        }

        private string GetCellName(int row, int col)
        {
            return GetColumnName(col + 1) + (row + 1).ToString();
        }

        private void Entry_Focused(object? sender, FocusEventArgs e)
        {
            if (sender is Entry entry)
            {
                currentSelectedEntry = entry; 
                currentSelectedCellName = entry.StyleId;

                if (cellMap.TryGetValue(currentSelectedCellName, out Cell? cell))
                {
                    textInput.Text = cell.Expression;
                    
                    entry.Text = cell.Expression; 
                }
                else
                {
                    textInput.Text = string.Empty;
                }
            }
        }

        private void Cell_ContentChanged(object? sender, EventArgs e)
        {
            if (sender is not Entry entry) return;

            if (cellMap.TryGetValue(entry.StyleId, out Cell? cell))
            {
                string newExpression = entry.Text;
                if (cell.Expression != newExpression)
                {
                    cell.Expression = newExpression;
                    cell.Value = double.NaN; 
                    
                    if (currentSelectedCellName == entry.StyleId)
                    {
                        textInput.Text = newExpression;
                    }
                }

                Dispatcher.Dispatch(() => UpdateCellDisplay(entry, cell));
            }
        }

        private void TextInput_Completed(object sender, EventArgs e)
        {
            if (currentSelectedEntry != null && !string.IsNullOrEmpty(currentSelectedCellName) && cellMap.TryGetValue(currentSelectedCellName, out Cell? cell))
            {
                string newExpression = textInput.Text;
                if (cell.Expression != newExpression)
                {
                    cell.Expression = newExpression;
                    cell.Value = double.NaN;
                    Dispatcher.Dispatch(() => UpdateCellDisplay(currentSelectedEntry, cell));
                }
            }
        }
        
        private void TextInput_Unfocused(object sender, FocusEventArgs e)
        {
            TextInput_Completed(sender, EventArgs.Empty);

            if (currentSelectedEntry != null && cellMap.TryGetValue(currentSelectedCellName, out Cell? cell))
            {
                UpdateCellDisplay(currentSelectedEntry, cell);
            }
        }

        private async void CalculateButton_Clicked(object sender, EventArgs e)
        {
            showExpression = !showExpression;

            if (!showExpression)
            {
                await RecalculateAllCells();
            }
            UpdateGridDisplay();
        }
        
        private async Task RecalculateCell(string cellName)
        {
            if (!cellMap.TryGetValue(cellName, out Cell? cell))
            {
                throw new ArgumentException($"ПОМИЛКА: Клітинка '{cellName}' не знайдена під час перерахунку.");
            }

            if (currentlyCalculating.Contains(cellName))
            {
                cell.Value = double.NaN;
                await DisplayAlert("Помилка", $"Виявлено циклічне посилання за участю клітинки {cellName}", "OK");
                return;
            }

            currentlyCalculating.Add(cellName);
            try
            {
                if (!string.IsNullOrEmpty(cell.Expression) && cell.Expression.StartsWith("="))
                {
                    string formula = cell.Expression.Substring(1);
                    if (string.IsNullOrWhiteSpace(formula))
                    {
                        cell.Value = 0.0;
                    }
                    else
                    {
                        cell.Value = await Calculator.Evaluate(formula, this);
                    }
                }
                else if (string.IsNullOrWhiteSpace(cell.Expression))
                {
                    cell.Value = 0.0;
                }
                else if (double.TryParse(cell.Expression, System.Globalization.CultureInfo.InvariantCulture,
                             out double directValue))
                {
                    cell.Value = directValue;
                }
                else
                {
                    cell.Value = double.NaN;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Помилка обчислення {cellName}: {ex.Message}");
                cell.Value = double.NaN;
                await DisplayAlert("Помилка обчислення", $"Помилка в клітинці {cellName}: {ex.Message}", "OK");
            }
            finally
            {
                currentlyCalculating.Remove(cellName);
            }
        }

        public async Task<double> GetCellValue(string cellName)
        {
            if (!cellMap.TryGetValue(cellName, out Cell? cell))
            {
                Debug.WriteLine($"Помилка: Клітинка '{cellName}'. Посилання не знайдено");
                return 0.0;
            }

            if (double.IsNaN(cell.Value))
            {
                await RecalculateCell(cellName);
            }

            return cell.Value;
        }

        private async Task RecalculateAllCells()
        {
            foreach (var cell in cellMap.Values)
            {
                cell.Value = double.NaN;
            }

            foreach (var cellName in cellMap.Keys.ToList())
            {
                if (cellMap.TryGetValue(cellName, out Cell? cell) && double.IsNaN(cell.Value))
                {
                    try
                    {
                        await RecalculateCell(cellName);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Помилка під час обчислювання клітинки: {cellName}: {ex.Message}");
                    }
                }
            }
        }
        
        private void UpdateCellDisplay(Entry entry, Cell cell)
        {
            if (showExpression)
            {
                entry.Text = cell.Expression;
            }
            else
            {
                if (double.IsNaN(cell.Value))
                {
                    if (!string.IsNullOrEmpty(cell.Expression) && !cell.Expression.StartsWith("=") && !double.TryParse(cell.Expression, out _))
                    {
                        entry.Text = cell.Expression;
                    }
                    else
                    {
                        entry.Text = "#ERR!"; 
                    }
                }
                else
                {
                    entry.Text = cell.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
        }

        private void UpdateGridDisplay()
        {
            foreach (var child in grid.Children)
            {
                if (child is Entry entry)
                {
                    if (cellMap.TryGetValue(entry.StyleId, out Cell? cell))
                    {
                        Dispatcher.Dispatch(() => UpdateCellDisplay(entry, cell));
                    }
                }
            }
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            Dictionary<string, string> dataToSave = new Dictionary<string, string>();
            foreach (var cellPair in cellMap)
            {
                if (!string.IsNullOrWhiteSpace(cellPair.Value.Expression))
                {
                    dataToSave[cellPair.Key] = cellPair.Value.Expression;
                }
            }

            string jsonString;
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                jsonString = JsonSerializer.Serialize(dataToSave, options);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", $"Не вдалося серіалізувати дані: {ex.Message}", "OK");
                return;
            }
#if WINDOWS
            try
            {
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("JSON Files", new List<string>() { ".json" });
                savePicker.SuggestedFileName = "MyExcelTable";
                var window = App.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (window == null) throw new InvalidOperationException("Не вдалося отримати вікно програми.");
                InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(window));
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, jsonString);
                    await DisplayAlert("Успіх", $"Файл збережено: {file.Path}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка збереження (Windows)", $"Сталася помилка: {ex.Message}", "OK");
            }
#else
            await DisplayAlert("Помилка", "Збереження файлів підтримується тільки на Windows у цій версії.", "OK");
#endif
        }

        private async void ReadButton_Clicked(object sender, EventArgs e)
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".json" } }, { DevicePlatform.MacCatalyst, new[] { "json" } },
                { DevicePlatform.Android, new[] { "application/json" } },
                { DevicePlatform.iOS, new[] { "public.json" } },
            });
            var pickOptions = new PickOptions
                { PickerTitle = "Виберіть файл .json для завантаження", FileTypes = customFileType, };
            try
            {
                FileResult? result = await FilePicker.Default.PickAsync(pickOptions);
                if (result == null)
                {
                    return;
                }

                string jsonString = await System.IO.File.ReadAllTextAsync(result.FullPath);
                var loadedData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                if (loadedData == null || loadedData.Count == 0)
                {
                    await DisplayAlert("Пустий файл", "Файл не містить даних для завантаження.", "OK");
                    return;
                }

                ClearGrid();
                foreach (var pair in loadedData)
                {
                    if (cellMap.TryGetValue(pair.Key, out Cell? cell))
                    {
                        cell.Expression = pair.Value;
                    }
                }
                showExpression = true;
                UpdateGridDisplay();
                await DisplayAlert("Успіх", "Дані успішно завантажено.", "OK");
            }
            catch (FileNotFoundException fnfEx) when (fnfEx.Message.Contains("FilePicker"))
            {
                await DisplayAlert("Помилка Конфігурації?",
                    $"Не вдалося отримати доступ до FilePicker: {fnfEx.Message}. Перевірте налаштування MAUI Essentials.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", $"Не вдалося прочитати або завантажити файл: {ex.Message}", "OK");
            }
        }

        private void ClearGrid()
        {
            foreach (var cell in cellMap.Values)
            {
                cell.Expression = "";
                cell.Value = double.NaN;
            }

            UpdateGridDisplay();
        }

        private async void ExitButton_Clicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Підтвердження", "Ви точно хочете вийти?", "Так", "Ні");
            if (answer)
            {
                Application.Current?.Quit();
            }
        }

        private async void HelpButton_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Довідка", "Лабораторна робота 1. Студентки Банасевич Вікторії", "OK");
        }

        private void DeleteRowButton_Clicked(object sender, EventArgs e)
        {
            if (currentRowCount > 0)
            {
                int lastRowDataIndex = currentRowCount - 1;
                int lastRowUIIndex = currentRowCount;
                if (lastRowDataIndex < cellData.Count)
                {
                    var rowToRemove = cellData[lastRowDataIndex];
                    foreach (var cell in rowToRemove)
                    {
                        cellMap.Remove(cell.Name);
                    }

                    cellData.RemoveAt(lastRowDataIndex);
                }

                List<IView> childrenToRemove = new List<IView>();
                foreach (var child in grid.Children)
                {
                    if (grid.GetRow(child) == lastRowUIIndex)
                    {
                        childrenToRemove.Add(child);
                    }
                }

                foreach (var child in childrenToRemove)
                {
                    grid.Children.Remove(child);
                }

                if (grid.RowDefinitions.Count > 1) grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);
                currentRowCount--;
                Task.Run(async () =>
                {
                    await RecalculateAllCells();
                    UpdateGridDisplay();
                });
            }
        }

        private void DeleteColumnButton_Clicked(object sender, EventArgs e)
        {
            if (currentColumnCount > 0)
            {
                int lastColDataIndex = currentColumnCount - 1;
                int lastColUIIndex = currentColumnCount;
                foreach (var rowData in cellData)
                {
                    if (lastColDataIndex < rowData.Count)
                    {
                        cellMap.Remove(rowData[lastColDataIndex].Name);
                        rowData.RemoveAt(lastColDataIndex);
                    }
                }

                List<IView> childrenToRemove = new List<IView>();
                foreach (var child in grid.Children)
                {
                    if (grid.GetColumn(child) == lastColUIIndex)
                    {
                        childrenToRemove.Add(child);
                    }
                }

                foreach (var child in childrenToRemove)
                {
                    grid.Children.Remove(child);
                }

                if (grid.ColumnDefinitions.Count > 1) grid.ColumnDefinitions.RemoveAt(grid.ColumnDefinitions.Count - 1);
                currentColumnCount--;
                Task.Run(async () =>
                {
                    await RecalculateAllCells();
                    UpdateGridDisplay();
                });
            }
        }

        private void AddRowButton_Clicked(object sender, EventArgs e)
        {
            int newRowUIIndex = currentRowCount + 1;
            int newRowDataIndex = currentRowCount;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var rowData = new List<Cell>();
            var label = new Label
            {
                Text = newRowUIIndex.ToString(), VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(5)
            };
            Grid.SetRow(label, newRowUIIndex);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);
            for (int col = 0; col < currentColumnCount; col++)
            {
                var cellName = GetCellName(newRowDataIndex, col);
                var cell = new Cell { Name = cellName, Value = double.NaN };
                rowData.Add(cell);
                cellMap[cellName] = cell;
                var entry = new Entry
                {
                    Text = "", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Fill,
                    StyleId = cellName
                };
             
                entry.Focused += Entry_Focused;
                entry.Completed += Cell_ContentChanged; 
                entry.Unfocused += Cell_ContentChanged;
                
                Grid.SetRow(entry, newRowUIIndex);
                Grid.SetColumn(entry, col + 1);
                grid.Children.Add(entry);
            }

            if (cellData.Count == newRowDataIndex)
            {
                cellData.Add(rowData);
            }
            else
            {
                cellData.Insert(newRowDataIndex, rowData);
            }

            currentRowCount++;
        }

        private void AddColumnButton_Clicked(object sender, EventArgs e)
        {
            int newColUIIndex = currentColumnCount + 1;
            int newColDataIndex = currentColumnCount;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var label = new Label
            {
                Text = GetColumnName(newColUIIndex), VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(5)
            };
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, newColUIIndex);
            grid.Children.Add(label);
            for (int row = 0; row < currentRowCount; row++)
            {
                var cellName = GetCellName(row, newColDataIndex);
                var cell = new Cell { Name = cellName, Value = double.NaN };
                if (row < cellData.Count)
                {
                    if (cellData[row].Count == newColDataIndex)
                    {
                        cellData[row].Add(cell);
                    }
                    else
                    {
                        cellData[row].Insert(newColDataIndex, cell);
                    }
                }

                cellMap[cellName] = cell;
                var entry = new Entry
                {
                    Text = "", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Fill,
                    StyleId = cellName
                };
                
                entry.Focused += Entry_Focused;
                entry.Completed += Cell_ContentChanged;
                entry.Unfocused += Cell_ContentChanged;
                
                Grid.SetRow(entry, row + 1);
                Grid.SetColumn(entry, newColUIIndex);
                grid.Children.Add(entry);
            }

            currentColumnCount++;
        }
    }
}