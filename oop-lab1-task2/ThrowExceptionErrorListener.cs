using Antlr4.Runtime;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oop_lab1_task2
{
    public class ThrowExceptionErrorListener : BaseErrorListener, IAntlrErrorListener<int>
    {
        public static readonly ThrowExceptionErrorListener Instance = new ThrowExceptionErrorListener();

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            throw new ArgumentException($"Invalid Expression (at token {offendingSymbol?.Text ?? "unknown"}): {msg} at line {line}:{charPositionInLine}", nameof(msg), e);
        }
        public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            throw new ArgumentException($"Invalid Expression (at character index {offendingSymbol}): {msg} at line {line}:{charPositionInLine}", nameof(msg), e);
        }
    }
}
