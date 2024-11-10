// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Extensions;

namespace RemoteMaster.Host.Core.Tests;

public class TypeExtensionsTests
{
    #region Non-Generic Types

    [Fact]
    public void GetFriendlyName_NonGenericType_ReturnsTypeName()
    {
        // Arrange
        var type = typeof(string);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("String", result);
    }

    [Fact]
    public void GetFriendlyName_NonGenericType_NamespaceIncluded_ReturnsTypeNameWithoutNamespace()
    {
        // Arrange
        var type = typeof(int);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("Int32", result);
    }

    #endregion

    #region Generic Types

    [Fact]
    public void GetFriendlyName_SimpleGenericType_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(List<int>);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("List<Int32>", result);
    }

    [Fact]
    public void GetFriendlyName_GenericTypeWithMultipleParameters_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(Dictionary<string, int>);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("Dictionary<String, Int32>", result);
    }

    [Fact]
    public void GetFriendlyName_NestedGenericType_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(Dictionary<string, List<int>>);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("Dictionary<String, List<Int32>>", result);
    }

    [Fact]
    public void GetFriendlyName_GenericTypeWithArray_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(List<int[]>);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("List<Int32[]>", result);
    }

    #endregion

    #region Null and Edge Cases

    [Fact]
    public void GetFriendlyName_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? type = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => type!.GetFriendlyName());
        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void GetFriendlyName_GenericTypeWithoutArguments_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(List<>);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("List<>", result);
    }

    [Fact]
    public void GetFriendlyName_DeeplyNestedGenericType_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(Dictionary<string, Dictionary<int, List<bool>>>);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("Dictionary<String, Dictionary<Int32, List<Boolean>>>", result);
    }

    [Fact]
    public void GetFriendlyName_ArrayType_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(int[]);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("Int32[]", result);
    }

    [Fact]
    public void GetFriendlyName_MultiDimensionalArrayType_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(int[,]);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("Int32[,]", result);
    }

    [Fact]
    public void GetFriendlyName_GenericTypeWithMultiDimensionalArray_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(List<int[,]>);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("List<Int32[,]>", result);
    }

    #endregion

    #region Complex Cases

    [Fact]
    public void GetFriendlyName_ComplexTypeWithNestedAndArray_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(Dictionary<string[], List<int[][]>>);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("Dictionary<String[], List<Int32[][]>>", result);
    }

    [Fact]
    public void GetFriendlyName_TypeWithMultipleLevelsOfGenerics_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(Tuple<List<int>, Dictionary<string, List<double>>>);

        // Act
        var result = type.GetFriendlyName();

        // Assert
        Assert.Equal("Tuple<List<Int32>, Dictionary<String, List<Double>>>", result);
    }

    #endregion
}
