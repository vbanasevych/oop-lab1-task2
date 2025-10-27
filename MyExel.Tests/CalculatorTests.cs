using System.Collections.Generic;
using System.Threading.Tasks;
using Spreadsheet.Core; 
using oop_lab1_task2;
using Xunit;

namespace MyExel.Tests 
{
    public class CalculatorTests
    {
        private readonly TestCellProvider _provider;

        public CalculatorTests()
        {
            _provider = new TestCellProvider();
            _provider.CellValues["A1"] = 10;
            _provider.CellValues["B2"] = 20;
            _provider.CellValues["C3"] = 5;
        }

        [Fact]
        public async Task Evaluate_SimpleAddition_ReturnsCorrectSum()
        {
            // Arrange
            string formula = "10 + 2 * 6 / (4 - 1)"; // 10 + 12 / 3 = 10 + 4 = 14

            // Act
            double result = await Calculator.Evaluate(formula, _provider);

            // Assert
            Assert.Equal(14, result);
        }
        
        [Fact]
        public async Task Evaluate_WithCellReferences_ReturnsCorrectSum()
        {
            // Arrange
            string formula = "A1+B2-2*3";

            // Act
            double result = await Calculator.Evaluate(formula, _provider);

            // Assert
            Assert.Equal(24, result);
        }

        [Theory]
        [InlineData("10/2", 5)]
        [InlineData("(A1*2)+B2", 40)]       
        [InlineData("B2/C3", 4)]            
        [InlineData("A1+B2+C3", 35)]        
        [InlineData("A1*C3-B2", 30)]        
        [InlineData("D4+A1", 10)]           
        public async Task Evaluate_VariousFormulas_ReturnsExpected(string formula, double expected)
        {
            // Act
            double result = await Calculator.Evaluate(formula, _provider);

            // Assert
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public async Task Evaluate_UnknownCell_ReturnsZero()
        {
            // Arrange
            string formula = "Z99"; 

            // Act
            double result = await Calculator.Evaluate(formula, _provider);

            // Assert
            Assert.Equal(0, result);
        }
    }
}