using System.Threading.Tasks;

namespace Spreadsheet.Core
{
    public interface ICellValueProvider
    {
        Task<double> GetCellValue(string cellName);
    }
}