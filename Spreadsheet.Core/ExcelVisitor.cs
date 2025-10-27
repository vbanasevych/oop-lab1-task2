using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Spreadsheet.Core
{
    public class ExcelVisitor : MyExelBaseVisitor<Task<double>>
    {
        private readonly ICellValueProvider _provider;

        public ExcelVisitor(ICellValueProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public override async Task<double> VisitCompileUnit(MyExelParser.CompileUnitContext context)
        {
            Debug.WriteLine("Visiting CompileUnit");
            return await Visit(context.expression());
        }

        public override Task<double> VisitNumberExpr(MyExelParser.NumberExprContext context)
        {
            var text = context.NUMBER().GetText();
            Debug.WriteLine($"Visiting NumberExpr: {text}");
            var result = double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
            return Task.FromResult(result);
        }

        public override async Task<double> VisitParenthesizedExpr(MyExelParser.ParenthesizedExprContext context)
        {
            Debug.WriteLine("Visiting ParenthesizedExpr");
            return await Visit(context.expression());
        }

        public override async Task<double> VisitUnaryExpr(MyExelParser.UnaryExprContext context)
        {
            Debug.WriteLine($"Visiting UnaryExpr: {context.op.Text}");
            var value = await Visit(context.expression()); 
            if (context.op.Type == MyExelLexer.SUBTRACT)
            {
                return -value;
            }
            return value;
        }

        public override async Task<double> VisitMultiplicativeExpr(MyExelParser.MultiplicativeExprContext context)
        {
            Debug.WriteLine($"Visiting MultiplicativeExpr: {context.op.Text}");
            var left = await Visit(context.expression(0));
            var right = await Visit(context.expression(1));

            switch (context.op.Type)
            {
                case MyExelLexer.MULTIPLY:
                    return left * right;
                case MyExelLexer.DIVIDE:
                    if (right == 0) throw new DivideByZeroException("Ділення на нуль.");
                    return left / right;
                case MyExelLexer.MOD:
                    if (right == 0) throw new DivideByZeroException("Ділення по модулю на нуль.");
                    return left % right;
                case MyExelLexer.DIV:
                    if (right == 0) throw new DivideByZeroException("Цілочисельне ділення на нуль.");
                    return Math.Truncate(left / right);
                default:
                    throw new InvalidOperationException($"Невідомий оператор: {context.op.Text}");
            }
        }

        public override async Task<double> VisitAdditiveExpr(MyExelParser.AdditiveExprContext context)
        {
            Debug.WriteLine($"Visiting AdditiveExpr: {context.op.Text}");
            var left = await Visit(context.expression(0));
            var right = await Visit(context.expression(1));

            if (context.op.Type == MyExelLexer.ADD) return left + right;
            return left - right; 
        }

        public override async Task<double> VisitFuncExpr(MyExelParser.FuncExprContext context)
        {
            Debug.WriteLine($"Visiting FuncExpr: {context.funcName.Text}");
            var value = await Visit(context.expression()); 
            
            if (context.funcName.Type == MyExelLexer.INC) return value + 1;
            return value - 1; 
        }

        public override async Task<double> VisitCellRefExpr(MyExelParser.CellRefExprContext context)
        {
            var cellName = context.CELL_REF().GetText().ToUpper();
            Debug.WriteLine($"Visiting CellRefExpr for: {cellName}");
            
            if (_provider == null)
            {
                throw new InvalidOperationException("Помилка: Провайдер комірок не ініціалізовано.");
            }
            
            try
            {
                double value = await _provider.GetCellValue(cellName);

                Debug.WriteLine($"GetCellValue({cellName}) returned: {value}");

                if (double.IsNaN(value))
                {
                    Debug.WriteLine($"GetCellValue({cellName}) returned NaN. Throwing specific error.");
                    throw new InvalidOperationException($"Посилання клітинки {cellName} призвело до помилки (значення NaN або циклічне посилання).");
                }

                Debug.WriteLine($"VisitCellRefExpr({cellName}) returning successfully: {value}");
                return value;
            }
            catch (InvalidOperationException cycleEx) when (cycleEx.Message.Contains("циклічне посилання"))
            {
                Debug.WriteLine($"VisitCellRefExpr({cellName}) caught circular reference: {cycleEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VisitCellRefExpr({cellName}) FAILED: Exception type {ex.GetType().Name} - {ex.Message}");
                throw new InvalidOperationException($"Помилка отримання значення клітинки {cellName}.", ex);
            }
        }
    }
}