// using Itinero.Data.Graphs.TurnCosts;
// using Xunit;
//
// namespace Itinero.Tests.Data.Graphs.TurnCosts
// {
//     public class TurnCostMatrixTests
//     {
//         [Fact]
//         public void TurnCostMatrix_New_N1_ShouldRowsColumns()
//         {
//             var matrix = new TurnCostMatrix(new uint[]
//             {
//                 0
//             });
//             
//             Assert.Equal(0U, matrix.Get(0, 0));
//         }
//         
//         [Fact]
//         public void TurnCostMatrix_New_N2_ShouldRowsColumns()
//         {
//             var matrix = new TurnCostMatrix(new uint[]
//             {
//                 0, 1, 
//                 2, 3
//             });
//             
//             Assert.Equal(0U, matrix.Get(0, 0));
//             Assert.Equal(1U, matrix.Get(0, 1));
//             Assert.Equal(2U, matrix.Get(1, 0));
//             Assert.Equal(3U, matrix.Get(1, 1));
//         }
//         
//         [Fact]
//         public void TurnCostMatrix_New_N3_ShouldRowsColumns()
//         {
//             var matrix = new TurnCostMatrix(new uint[]
//             {
//                 0, 1, 2,
//                 3, 4, 5,
//                 6, 7, 8
//             });
//             
//             Assert.Equal(0U, matrix.Get(0, 0));
//             Assert.Equal(1U, matrix.Get(0, 1));
//             Assert.Equal(2U, matrix.Get(0, 2));
//             Assert.Equal(3U, matrix.Get(1, 0));
//             Assert.Equal(4U, matrix.Get(1, 1));
//             Assert.Equal(5U, matrix.Get(1, 2));
//             Assert.Equal(6U, matrix.Get(2, 0));
//             Assert.Equal(7U, matrix.Get(2, 1));
//             Assert.Equal(8U, matrix.Get(2, 2));
//         }
//         
//         [Fact]
//         public void TurnCostMatrix_Sort_N1_ShouldBeOneElement()
//         {
//             var matrix = new TurnCostMatrix(new uint[]
//             {
//                 0
//             });
//             
//             matrix.Sort();
//             
//             Assert.Equal(0U, matrix.Get(0,0));
//         }
//         
//         [Fact]
//         public void TurnCostMatrix_Sort_N2_ShouldBeOneElement()
//         {
//             var matrix = new TurnCostMatrix(new uint[]
//             {
//                 0, 1, 
//                 2, 3
//             });
//             
//             matrix.Sort();
//             
//             Assert.Equal(1, matrix.OriginalRow(0));
//             Assert.Equal(0, matrix.OriginalRow(1));
//             Assert.Equal(1, matrix.OriginalColumn(0));
//             Assert.Equal(0, matrix.OriginalColumn(1));
//             
//             Assert.Equal(3U, matrix.Get(0,0));
//             Assert.Equal(2U, matrix.Get(0,1));
//             Assert.Equal(1U, matrix.Get(1,0));
//             Assert.Equal(0U, matrix.Get(1,1));
//         }
//         
//         [Fact]
//         public void TurnCostMatrix_Sort_N3_ShouldBeOneElement()
//         {
//             var matrix = new TurnCostMatrix(new uint[]
//             {
//                 0, 1, 2,
//                 3, 4, 5,
//                 6, 7, 8
//             });
//             
//             matrix.Sort();
//             
//             Assert.Equal(2, matrix.OriginalRow(0));
//             Assert.Equal(1, matrix.OriginalRow(1));
//             Assert.Equal(0, matrix.OriginalRow(2));
//             Assert.Equal(2, matrix.OriginalRow(0));
//             Assert.Equal(1, matrix.OriginalRow(1));
//             Assert.Equal(0, matrix.OriginalRow(2));
//             
//             Assert.Equal(8U, matrix.Get(0,0));
//             Assert.Equal(7U, matrix.Get(0,1));
//             Assert.Equal(6U, matrix.Get(0,2));
//             Assert.Equal(5U, matrix.Get(1,0));
//             Assert.Equal(4U, matrix.Get(1,1));
//             Assert.Equal(3U, matrix.Get(1,2));
//             Assert.Equal(2U, matrix.Get(2,0));
//             Assert.Equal(1U, matrix.Get(2,1));
//             Assert.Equal(0U, matrix.Get(2,2));
//         }
//     }
// }

