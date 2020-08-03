// using Hudl.Mjolnir.Breaker;
// using System;
// using System.Collections.Generic;
// using Xunit;

// namespace Hudl.Mjolnir.Tests.Breaker
// {
//     public class IgnoredExceptionHandlerTests
//     {
//         [Fact]
//         public void IsExceptionIgnored_IgnoresExceptionsFromSet()
//         {
//             // Arrange

//             var ignored = new HashSet<Type>
//             {
//                 typeof(ArgumentNullException)
//             };
//             var handler = new IgnoredExceptionHandler(ignored);

//             // Act

//             var isIgnored = handler.IsExceptionIgnored(typeof(ArgumentNullException));

//             // Assert

//             Assert.True(isIgnored);
//         }

//         [Fact]
//         public void IsExceptionIgnored_AllowsExceptionsNotInSet()
//         {
//             // Arrange

//             var ignored = new HashSet<Type>
//             {
//                 typeof(ArgumentNullException)
//             };
//             var handler = new IgnoredExceptionHandler(ignored);

//             // Act

//             var isIgnored = handler.IsExceptionIgnored(typeof(InvalidOperationException));

//             // Assert

//             Assert.False(isIgnored);
//         }

//         [Fact]
//         public void IsExceptionIgnored_RequiresSpecificExceptionsAndDoesntIgnoreInherited()
//         {
//             // Arrange

//             var ignored = new HashSet<Type>
//             {
//                 typeof(ArgumentException)
//             };
//             var handler = new IgnoredExceptionHandler(ignored);

//             // Act

//             var isIgnored = handler.IsExceptionIgnored(typeof(ArgumentNullException));

//             // Assert

//             Assert.False(isIgnored);
//         }

//         [Fact]
//         public void Constructor_DerfensivelyCopiesSet()
//         {
//             // Arrange

//             var ignored = new HashSet<Type>();
//             var handler = new IgnoredExceptionHandler(ignored);

//             // Act
            
//             ignored.Add(typeof(ArgumentOutOfRangeException));
//             var isIgnored = handler.IsExceptionIgnored(typeof(ArgumentOutOfRangeException));

//             // Assert

//             Assert.False(isIgnored);
//         }
//     }
// }
