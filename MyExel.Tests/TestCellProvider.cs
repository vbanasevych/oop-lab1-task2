using System.Collections.Generic;
using System.Threading.Tasks;
using oop_lab1_task2; 
using Spreadsheet.Core;

public class TestCellProvider : ICellValueProvider
{
    // "фейкові" значення комірок
    public Dictionary<string, double> CellValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<double> GetCellValue(string cellName)
    {
        if (CellValues.TryGetValue(cellName, out double value))
        {
            return Task.FromResult(value);
        }
        // Повертаємо 0, якщо комірка не визначена в тесті
        return Task.FromResult(0.0);
    }
}