// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Updater.Controllers;

namespace RemoteMaster.Updater.Tests;

public class DecryptMethodTests
{
    [Fact]
    public void Decrypt_ShouldReturnCorrectString_WhenInputIsValid()
    {
        var input = "D9DDC3DBCCF4CDC3C9DEDCD8CCC3DDF4DEDCDAC78BC1";
        var shift = 3;
        var xorConstant = (byte)0xAB;
        var expectedOutput = "some_decrypted_string";

        var actualOutput = UpdateController.Decrypt(input, shift, xorConstant);

        Assert.Equal(expectedOutput, actualOutput);
    }

    [Fact]
    public void Decrypt_ShouldThrowException_WhenInputIsInvalid()
    {
        var invalidInput = "invalid_encrypted_string";
        var shift = 3;
        var xorConstant = (byte)0xAB;

        Assert.Throws<FormatException>(() => UpdateController.Decrypt(invalidInput, shift, xorConstant));
    }
}
