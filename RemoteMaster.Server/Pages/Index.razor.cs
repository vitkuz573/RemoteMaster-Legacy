// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Components;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;
using TypedSignalR.Client;

namespace RemoteMaster.Server.Pages;

public partial class Index
{
    private Dictionary<Computer, string> _scriptResults = new();
    private readonly List<Node> _entries = new();
    private Node _selectedNode;

    private bool _anyComputerSelected = false;
    private readonly List<Computer> _selectedComputers = new();

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Inject]
    private IConnectionManager ConnectionManager { get; set; }

    [Inject]
    private IWakeOnLanService WakeOnLanService { get; set; }

    [Inject]
    private IHttpClientFactory HttpClientFactory { get; set; }

    [Inject]
    private DialogService DialogService { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    protected async override Task OnInitializedAsync()
    {
        var rootFolders = (await DatabaseService.GetNodesAsync(f => f.Parent == null && f is Folder)).Cast<Folder>().ToList();

        foreach (var folder in rootFolders)
        {
            await LoadChildrenAsync(folder);
            _entries.Add(folder);
        }
    }

    private void HandleComputerSelection(Computer computer, bool isSelected)
    {
        if (isSelected)
        {
            _selectedComputers.Add(computer);
        }
        else
        {
            _selectedComputers.Remove(computer);
        }

        _anyComputerSelected = _selectedComputers.Any();
        StateHasChanged();
    }

    private async Task LoadChildrenAsync(Folder folder)
    {
        var children = await DatabaseService.GetChildrenByParentIdAsync<Folder>(folder.NodeId);

        foreach (var child in children)
        {
            folder.Children.Add(child);
            await LoadChildrenAsync(child);
        }
    }

    private void LoadComputers(TreeExpandEventArgs args)
    {
        var node = args.Value as Node;

        args.Children.Data = GetChildrenForNodeAsync(node).Result;
        args.Children.Text = GetTextForNode;
        args.Children.HasChildren = n => n is Folder && n is Node node && DatabaseService.HasChildrenAsync(node).Result;
        args.Children.Template = NodeTemplate;
    }

    private async Task<IEnumerable<Node>> GetChildrenForNodeAsync(Node node)
    {
        return node is Folder ? await DatabaseService.GetChildrenByParentIdAsync<Node>(node.NodeId) : Enumerable.Empty<Node>();
    }

    private readonly RenderFragment<RadzenTreeItem> NodeTemplate = (context) => builder =>
    {
        var icon = context.Value is Computer ? "desktop_windows" : "folder";
        var name = context.Value is Computer computer ? computer.Name : (context.Value as Folder)?.Name;

        builder.OpenComponent<RadzenIcon>(0);
        builder.AddAttribute(1, "Icon", icon);
        builder.CloseComponent();
        builder.AddContent(2, $" {name}");
    };

    private string GetTextForNode(object data) => data as string;

    private async Task OnTreeChange(TreeEventArgs args)
    {
        _selectedComputers.Clear();
        _anyComputerSelected = false;

        var node = args.Value as Node;

        if (node is Folder)
        {
            _selectedNode = node;
            await UpdateComputersThumbnailsAsync(node.Children.OfType<Computer>());
        }

        StateHasChanged();
    }

    private async Task UpdateComputersThumbnailsAsync(IEnumerable<Computer> computers)
    {
        var tasks = computers.Select(UpdateComputerThumbnailAsync).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task UpdateComputerThumbnailAsync(Computer computer)
    {
        Console.WriteLine($"UpdateComputerThumbnailAsync Called for {computer.IPAddress}");

        var clientContext = ConnectionManager.Connect("Client", $"http://{computer.IPAddress}:5076/hubs/control", true);

        try
        {
            clientContext.On<byte[]>("ReceiveThumbnail", async (thumbnailBytes) =>
            {
                if (thumbnailBytes?.Length > 0)
                {
                    computer.Thumbnail = thumbnailBytes;
                    await InvokeAsync(StateHasChanged);
                }
            });

            await clientContext.StartAsync();

            var proxy = clientContext.Connection.CreateHubProxy<IControlHub>();
            Console.WriteLine($"Calling ConnectAs with Intention.GetThumbnail for {computer.IPAddress}");
            await proxy.ConnectAs(Intention.GetThumbnail);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in UpdateComputerThumbnailAsync for {computer.IPAddress}: {ex.Message}");
        }
    }

    private async Task HandleRefreshClick()
    {
        if (_selectedNode is Folder selectedFolder)
        {
            await UpdateComputersThumbnailsAsync(selectedFolder.Children.OfType<Computer>());
        }
    }

    private async Task OpenWindow(string url)
    {
        await JSRuntime.InvokeVoidAsync("openNewWindow", url);
    }

    private async Task ExecuteOnAvailableComputers(Func<Computer, IControlHub, Task> actionOnComputer)
    {
        var tasks = _selectedComputers.Select(IsComputerAvailable).ToArray();
        var results = await Task.WhenAll(tasks);

        var availableComputers = results.Where(r => r.isAvailable);

        foreach (var (computer, isAvailable) in availableComputers)
        {
            var clientContext = ConnectionManager.Connect("Client", $"http://{computer.IPAddress}:5076/hubs/control", true);

            clientContext.On<string>("ReceiveScriptResult", async (result) =>
            {
                _scriptResults[computer] = result;

                await InvokeAsync(StateHasChanged);
            });

            await clientContext.StartAsync();

            var proxy = clientContext.Connection.CreateHubProxy<IControlHub>();
            await actionOnComputer(computer, proxy);
        }

        StateHasChanged();
    }

    private async Task Connect(RadzenSplitButtonItem item)
    {
        if (item == null)
        {
            return;
        }

        if (item.Value == "control")
        {
            await ExecuteOnAvailableComputers(async (computer, proxy) => await OpenWindow($"/{computer.IPAddress}/connect?imageQuality=25&cursorTracking=false&inputEnabled=true"));
        }

        if (item.Value == "view")
        {
            await ExecuteOnAvailableComputers(async (computer, proxy) => await OpenWindow($"/{computer.IPAddress}/connect?imageQuality=25&cursorTracking=true&inputEnabled=false"));
        }
    }

    private async Task OpenShell(RadzenSplitButtonItem item)
    {
        if (item == null)
        {
            return;
        }

        await ExecuteOnAvailableComputers(async (computer, proxy) =>
        {
            ProcessStartInfo startInfo;

            if (item.Value == "ssh")
            {
                var command = $"ssh user@{computer.IPAddress}";
                startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    UseShellExecute = true,
                };
            }
            else
            {
                var sParameter = item.Text.Contains("System") ? "-s" : "";
                var command = @$"/C psexec \\{computer.IPAddress} {sParameter} -nobanner -accepteula {item.Value}";
                startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = command,
                    UseShellExecute = true,
                };
            }

            await Task.Run(() => Process.Start(startInfo));
        });
    }

    private async Task Power(RadzenSplitButtonItem item)
    {
        if (item == null)
        {
            return;
        }

        if (item.Value == "shutdown")
        {
            await ExecuteOnAvailableComputers(async (computer, proxy) => await proxy.ShutdownComputer("", 0, true));
        }
        else if (item.Value == "reboot")
        {
            await ExecuteOnAvailableComputers(async (computer, proxy) => await proxy.RebootComputer("", 0, true));
        }
        else if (item.Value == "wakeup")
        {
            foreach (var computer in _selectedComputers)
            {
                WakeOnLanService.WakeUp(computer.MACAddress);
            }
        }
    }

    private async Task Update()
    {
        await ExecuteOnAvailableComputers(async (computer, proxy) =>
        {
            var client = HttpClientFactory.CreateClient();
            client.BaseAddress = new Uri($"http://{computer.IPAddress}:5124");

            var shift = 3;
            byte xorConstant = 0xAB;

            var values = new
            {
                login = Encrypt("support@it-ktk.local", shift, xorConstant),
                password = Encrypt("bonesgamer123!!", shift, xorConstant),
                sharedFolder = @"\\SERVER-DC02\Win\RemoteMaster"
            };

            var json = JsonSerializer.Serialize(values);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("/api/update", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        });
    }

    private async Task ScreenRecording(RadzenSplitButtonItem item)
    {
        if (item == null)
        {
            return;
        }

        if (item.Value == "start")
        {
            await ExecuteOnAvailableComputers(async (computer, proxy) =>
            {
                var requesterName = Environment.MachineName;
                var currentDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $@"C:\{requesterName}_{computer.IPAddress}_{currentDate}.mp4";

                await proxy.StartScreenRecording(fileName);
            });
        }
        else
        {
            await ExecuteOnAvailableComputers(async (computer, proxy) => await proxy.StopScreenRecording());
        }
    }

    private async Task SetMonitorState(RadzenSplitButtonItem item)
    {
        if (item == null)
        {
            return;
        }

        var state = item.Value switch
        {
            "on" => MonitorState.On,
            "standby" => MonitorState.Standby,
            "off" => MonitorState.Off
        };

        await ExecuteOnAvailableComputers(async (computer, proxy) => await proxy.SetMonitorState(state));
    }

    private async Task ExecuteScript()
    {
        var fileData = await JSRuntime.InvokeAsync<JsonElement>("selectFile");

        if (fileData.TryGetProperty("content", out var contentElement)
            && fileData.TryGetProperty("name", out var nameElement))
        {
            var fileContent = contentElement.GetString();
            var fileName = nameElement.GetString();

            var extension = Path.GetExtension(fileName);
            string shellType;

            switch (extension)
            {
                case ".ps1":
                    shellType = "PowerShell";
                    break;
                case ".bat":
                case ".cmd":
                    shellType = "CMD";
                    break;
                default:
                    Console.WriteLine("Unknown script type.");
                    return;
            }

            await ExecuteOnAvailableComputers(async (computer, proxy) => await proxy.ExecuteScript(fileContent, shellType));

            var dialogParameters = new Dictionary<string, object>
            {
                { "Results", _scriptResults }
            };

            var dialogOptions = new DialogOptions
            {
                Draggable = true
            };

            await DialogService.OpenAsync<ScriptResults>("Script Results", dialogParameters, dialogOptions);
        }
    }

    private async Task ManagePSExecRules(RadzenSplitButtonItem item)
    {
        if (item == null)
        {
            return;
        }

        bool isEnabled;

        if (bool.TryParse(item.Value, out isEnabled))
        {
            await ExecuteOnAvailableComputers(async (computer, proxy) => await proxy.SetPSExecRules(isEnabled));
        }
        else
        {
            Console.WriteLine($"Failed to convert the value: {item.Value}");
        }
    }

    private static async Task<(Computer computer, bool isAvailable)> IsComputerAvailable(Computer computer)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(computer.IPAddress, 1000);

            return (computer, reply.Status == IPStatus.Success);
        }
        catch
        {
            return (computer, false);
        }
    }

    private async Task OpenClientConfigGenerator()
    {
        var dialogOptions = new DialogOptions
        {
            Draggable = true
        };

        await DialogService.OpenAsync<ClientConfigurationGenerator>("Client Configuration Generator", null, dialogOptions);
    }

    public static string Encrypt(string input, int shift, byte xorConstant)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        string EncryptCaesar(string input, int shift)
        {
            var result = new StringBuilder(input.Length);

            foreach (var c in input)
            {
                if (char.IsLetter(c))
                {
                    var offset = char.IsUpper(c) ? 'A' : 'a';
                    result.Append((char)((c + shift - offset) % 26 + offset));
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        string Permute(string input)
        {
            if (input.Length % 2 != 0)
            {
                input += " ";
            }

            var result = new StringBuilder(input.Length);

            for (var i = 0; i < input.Length; i += 2)
            {
                result.Append(input[i + 1]);
                result.Append(input[i]);
            }

            return result.ToString();
        }

        var caesarEncrypted = EncryptCaesar(input, shift);
        var permuted = Permute(caesarEncrypted);
        var bytes = Encoding.UTF8.GetBytes(permuted);

        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] ^= xorConstant;
        }

        return BitConverter.ToString(bytes).Replace("-", "");
    }
}
