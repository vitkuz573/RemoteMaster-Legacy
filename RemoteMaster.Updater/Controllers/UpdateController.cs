// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Updater.Abstractions;
using RemoteMaster.Updater.Models;

namespace RemoteMaster.Updater.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UpdateController : ControllerBase
{
    private readonly IEnumerable<IComponentUpdater> _componentUpdaters;

    public UpdateController(IEnumerable<IComponentUpdater> componentUpdaters)
    {
        _componentUpdaters = componentUpdaters;
    }

    [HttpGet]
    public async Task<IActionResult> CheckForUpdates([FromQuery] UpdateRequest updateRequest)
    {
        if (updateRequest == null)
        {
            var errorResponse = new ErrorResponse
            {
                ErrorMessage = "UpdateRequest is null."
            };

            return BadRequest(errorResponse);
        }

        var shift = 3;
        byte xorConstant = 0xAB;

        try
        {
            updateRequest.Login = Decrypt(updateRequest.Login, shift, xorConstant);
            updateRequest.Password = Decrypt(updateRequest.Password, shift, xorConstant);
        }
        catch (Exception ex)
        {
            var errorResponse = new ErrorResponse
            {
                ErrorMessage = $"Decryption failed: {ex.Message}"
            };

            return BadRequest(errorResponse);
        }

        if (string.IsNullOrWhiteSpace(updateRequest.SharedFolder) || string.IsNullOrWhiteSpace(updateRequest.Login) || string.IsNullOrWhiteSpace(updateRequest.Password))
        {
            var errorResponse = new ErrorResponse
            {
                ErrorMessage = "Required parameters (sharedFolder, login, password) are missing."
            };

            return BadRequest(errorResponse);
        }

        var updateResults = new List<UpdateResponse>();

        foreach (var updater in _componentUpdaters)
        {
            try
            {
                var result = await updater.IsUpdateAvailableAsync(updateRequest.SharedFolder, updateRequest.Login, updateRequest.Password);
                updateResults.Add(result);
            }
            catch (Exception ex)
            {
                var response = new UpdateResponse
                {
                    ComponentName = updater.ComponentName,
                    Error = new ErrorResponse
                    {
                        ErrorMessage = ex.Message,
                        StackTrace = ex.StackTrace
                    }
                };

                updateResults.Add(response);
            }
        }

        return Ok(updateResults);
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] UpdateRequest updateRequest)
    {
        if (updateRequest == null)
        {
            var errorResponse = new ErrorResponse
            {
                ErrorMessage = "UpdateRequest is null."
            };

            return BadRequest(errorResponse);
        }

        var shift = 3;
        byte xorConstant = 0xAB;

        try
        {
            updateRequest.Login = Decrypt(updateRequest.Login, shift, xorConstant);
            updateRequest.Password = Decrypt(updateRequest.Password, shift, xorConstant);
        }
        catch (Exception ex)
        {
            var errorResponse = new ErrorResponse
            {
                ErrorMessage = $"Decryption failed: {ex.Message}"
            };

            return BadRequest(errorResponse);
        }

        if (string.IsNullOrWhiteSpace(updateRequest.SharedFolder) || string.IsNullOrWhiteSpace(updateRequest.Login) || string.IsNullOrWhiteSpace(updateRequest.Password))
        {
            var errorResponse = new ErrorResponse
            {
                ErrorMessage = "Required parameters (sharedFolder, login, password) are missing."
            };

            return BadRequest(errorResponse);
        }

        var updateResults = new List<UpdateResponse>();

        foreach (var updater in _componentUpdaters)
        {
            var response = new UpdateResponse
            {
                ComponentName = updater.ComponentName
            };

            try
            {
                var updateCheckResponse = await updater.IsUpdateAvailableAsync(updateRequest.SharedFolder, updateRequest.Login, updateRequest.Password);

                if (updateCheckResponse.IsUpdateAvailable)
                {
                    await updater.UpdateAsync(updateRequest.SharedFolder, updateRequest.Login, updateRequest.Password);
                    response.Message = "Update completed successfully.";
                }
                else
                {
                    response.Message = "No updates available.";
                }

                response.CurrentVersion = updateCheckResponse.CurrentVersion;
            }
            catch (Exception ex)
            {
                response.Error = new ErrorResponse
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace
                };
            }

            updateResults.Add(response);
        }

        return Ok(updateResults);
    }

    public static string Decrypt(string input, int shift, byte xorConstant)
    {
        string DecryptCaesar(string input, int shift)
        {
            var result = new StringBuilder(input.Length);

            foreach (var c in input)
            {
                if (char.IsLetter(c))
                {
                    var offset = char.IsUpper(c) ? 'A' : 'a';
                    result.Append((char)((c - shift - offset + 26) % 26 + offset));
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        string ReversePermute(string input)
        {
            var result = new StringBuilder(input.Length);

            for (var i = 0; i < input.Length; i += 2)
            {
                result.Append(input[i + 1]);
                result.Append(input[i]);
            }

            if (result[^1] == ' ')
            {
                result.Remove(result.Length - 1, 1);
            }

            return result.ToString();
        }

        byte[] StringToByteArray(string hex)
        {
            if (hex == null)
            {
                throw new ArgumentNullException(nameof(hex));
            }

            var numberChars = hex.Length;
            var bytes = new byte[numberChars / 2];

            for (var i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        var hexDecoded = StringToByteArray(input);

        for (var i = 0; i < hexDecoded.Length; i++)
        {
            hexDecoded[i] ^= xorConstant;
        }

        var xored = Encoding.UTF8.GetString(hexDecoded);
        var reversed = ReversePermute(xored);

        return DecryptCaesar(reversed, shift);
    }
}