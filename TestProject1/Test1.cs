using oop_lab1_task2;
using System;

namespace TestProject1;

[TestClass]
public sealed class Test1
{
   [TestClass]
    public class CalculatorTests
    {
        // Test 1: Basic arithmetic and operator precedence
        [TestMethod]
        public void Test_Arithmetic_Parentheses_Precedence()
        {
            // Arrange
            string expression = "10 * (6 - 2) / 4 + 1"; // 10 * 4 / 4 + 1 = 40 / 4 + 1 = 10 + 1 = 11
            double expected = 11.0;

            // Act
            double actual = Calculator.Evaluate(expression, null); // Pass null context

            // Assert
            Assert.AreEqual(expected, actual, $"Expression: {expression}");
        }

        // Test 2: 'mod' operator
        [TestMethod]
        public void Test_ModOperator_Simple()
        {
            // Arrange
            string expression = "15 mod 4"; // Expected: 3
            double expected = 3.0;

            // Act
            double actual = Calculator.Evaluate(expression, null); // Pass null context

            // Assert
            Assert.AreEqual(expected, actual, $"Expression: {expression}");
        }

        // Test 3: 'inc' function with nested calculation
        [TestMethod]
        public void Test_IncFunction_Nested()
        {
            // Arrange
            string expression = "inc(5 * 2 + 1)"; // inc(11) = 12
            double expected = 12.0;

            // Act
            double actual = Calculator.Evaluate(expression, null); // Pass null context

            // Assert
            Assert.AreEqual(expected, actual, $"Expression: {expression}");
        }

        // --- Additional Useful Tests ---

        // Test 4: Syntax Error
        [TestMethod]
        public void Test_SyntaxError_ThrowsArgumentException()
        {
             // Arrange
             string expression = "5++"; // Invalid

             // Act & Assert
             // Expect ArgumentException because ThrowExceptionErrorListener throws it
             Assert.ThrowsException<ArgumentException>(() => Calculator.Evaluate(expression, null));
        }

        // Test 5: Integer division 'div'
        [TestMethod]
        public void Test_DivOperator_IntegerResult()
        {
            // Arrange
            string expression = "17 div 5"; // Expected: 3
            double expected = 3.0;

            // Act
            double actual = Calculator.Evaluate(expression, null);

            // Assert
            Assert.AreEqual(expected, actual, $"Expression: {expression}");
        }
    }
}