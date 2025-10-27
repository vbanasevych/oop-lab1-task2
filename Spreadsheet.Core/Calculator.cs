using System;
using System.Threading.Tasks; 
using Antlr4.Runtime;
using Spreadsheet.Core; 
namespace Spreadsheet.Core 
{
    public static class Calculator
    {
        // 1. ЗМІНЮЄМО СИГНАТУРУ:
        //    - 'async Task<double>' замість 'double'
        //    - 'ICellValueProvider provider' замість 'MainPage? context'
        public static async Task<double> Evaluate(string expression, ICellValueProvider provider)
        {
            var lexer = new MyExelLexer(new AntlrInputStream(expression));
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(ThrowExceptionErrorListener.Instance); // Використовуємо Singleton

            var tokens = new CommonTokenStream(lexer);

            var parser = new MyExelParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(ThrowExceptionErrorListener.Instance); // Використовуємо Singleton

            var tree = parser.compileUnit();

            // Передаємо 'provider' (інтерфейс), а не 'context'
            var visitor = new ExcelVisitor(provider);

            // 2. ЗМІНЮЄМО ВИКЛИК:
            //    Додаємо 'await', оскільки Visit() тепер повертає Task<double>
            return await visitor.Visit(tree);
        }
    }
}