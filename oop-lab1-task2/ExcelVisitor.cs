using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace oop_lab1_task2
{
    public class ExcelVisitor : MyExelBaseVisitor<double>
    {
        private readonly MainPage mainPageContext;

        public ExcelVisitor(MainPage context)
        {
            mainPageContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public override double VisitCompileUnit(MyExelParser.CompileUnitContext context)
        {
            Debug.WriteLine("Visiting CompileUnit");
            return Visit(context.expression());
        }

        public override double VisitNumberExpr(MyExelParser.NumberExprContext context)
        {
            var text = context.NUMBER().GetText();
            Debug.WriteLine($"Visiting NumberExpr: {text}");
            return double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }

        public override double VisitParenthesizedExpr(MyExelParser.ParenthesizedExprContext context)
        {
            Debug.WriteLine("Visiting ParenthesizedExpr");
            return Visit(context.expression());
        }

        public override double VisitUnaryExpr(MyExelParser.UnaryExprContext context)
        {
            Debug.WriteLine($"Visiting UnaryExpr: {context.op.Text}");
            var value = Visit(context.expression());
            if (context.op.Type == MyExelLexer.SUBTRACT)
            {
                return -value;
            }
            return value;
        }

        public override double VisitMultiplicativeExpr(MyExelParser.MultiplicativeExprContext context)
        {
            Debug.WriteLine($"Visiting MultiplicativeExpr: {context.op.Text}");
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));

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

        public override double VisitAdditiveExpr(MyExelParser.AdditiveExprContext context)
        {
            Debug.WriteLine($"Visiting AdditiveExpr: {context.op.Text}");
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));

            if (context.op.Type == MyExelLexer.ADD) return left + right;
            return left - right; 
        }

        public override double VisitFuncExpr(MyExelParser.FuncExprContext context)
        {
            Debug.WriteLine($"Visiting FuncExpr: {context.funcName.Text}");
            var value = Visit(context.expression());

            if (context.funcName.Type == MyExelLexer.INC) return value + 1;
            return value - 1; 
        }

        public override double VisitCellRefExpr(MyExelParser.CellRefExprContext context)
        {
            var cellName = context.CELL_REF().GetText().ToUpper();
            Debug.WriteLine($"Visiting CellRefExpr for: {cellName}");

            try
            {
                Task<double> valueTask = mainPageContext.GetCellValue(cellName);
                double value = valueTask.GetAwaiter().GetResult();

                Debug.WriteLine($"GetCellValue({cellName}) returned: {value}");

                if (double.IsNaN(value))
                {
                    Debug.WriteLine($"GetCellValue({cellName}) returned NaN. Throwing specific error.");
                    throw new InvalidOperationException($"Посилання клітинки {cellName} призвело до помилки (значення NaN).");
                }

                Debug.WriteLine($"VisitCellRefExpr({cellName}) returning successfully: {value}");
                return value;
            }
            
            catch (InvalidOperationException cycleEx) when (cycleEx.Message.Contains("Circular reference"))
            {
                Debug.WriteLine($"VisitCellRefExpr({cellName}) caught circular reference: {cycleEx.Message}");
            }

            catch (Exception ex)
            {
                Debug.WriteLine($"VisitCellRefExpr({cellName}) FAILED: Exception type {ex.GetType().Name} - {ex.Message}");
                throw new InvalidOperationException($"Помилка отримання значення клітинки {cellName}.", ex);
            }

            return double.NaN;
        }
    }
}