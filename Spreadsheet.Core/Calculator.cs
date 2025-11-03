using System;
using System.Threading.Tasks; 
using Antlr4.Runtime;
using Spreadsheet.Core; 
namespace Spreadsheet.Core 
{
    public static class Calculator
    {
        public static async Task<double> Evaluate(string expression, ICellValueProvider provider)
        {
            var lexer = new MyExelLexer(new AntlrInputStream(expression));
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(ThrowExceptionErrorListener.Instance);

            var tokens = new CommonTokenStream(lexer);

            var parser = new MyExelParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(ThrowExceptionErrorListener.Instance);

            var tree = parser.compileUnit();

            var visitor = new ExcelVisitor(provider);
            
            return await visitor.Visit(tree);
        }
    }
}