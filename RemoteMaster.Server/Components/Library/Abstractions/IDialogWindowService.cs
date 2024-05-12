// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library.Abstractions;

public interface IDialogWindowService
{
    event Action<IDialogReference, DialogResult> OnDialogCloseRequested;

    event Action<IDialogReference> OnDialogInstanceAdded;

    void Close(IDialogReference dialog);

    void Close(IDialogReference dialog, DialogResult result);

    IDialogReference CreateReference();

    IDialogReference Show(Type contentComponent);

    IDialogReference Show(Type contentComponent, string title);

    IDialogReference Show(Type contentComponent, string title, DialogOptions options);

    IDialogReference Show(Type contentComponent, string title, DialogParameters parameters);

    IDialogReference Show(Type contentComponent, string title, DialogParameters parameters, DialogOptions options);

    IDialogReference Show<T>() where T : IComponent;

    IDialogReference Show<T>(string title) where T : IComponent;

    IDialogReference Show<T>(string title, DialogOptions options) where T : IComponent;

    IDialogReference Show<T>(string title, DialogParameters parameters) where T : IComponent;

    IDialogReference Show<T>(string title, DialogParameters parameters, DialogOptions options) where T : IComponent;

    Task<IDialogReference> ShowAsync(Type contentComponent);

    Task<IDialogReference> ShowAsync(Type contentComponent, string title);

    Task<IDialogReference> ShowAsync(Type contentComponent, string title, DialogOptions options);

    Task<IDialogReference> ShowAsync(Type contentComponent, string title, DialogParameters parameters);

    Task<IDialogReference> ShowAsync(Type contentComponent, string title, DialogParameters parameters, DialogOptions options);

    Task<IDialogReference> ShowAsync<T>() where T : IComponent;

    Task<IDialogReference> ShowAsync<T>(string title) where T : IComponent;

    Task<IDialogReference> ShowAsync<T>(string title, DialogOptions options) where T : IComponent;

    Task<IDialogReference> ShowAsync<T>(string title, DialogParameters parameters) where T : IComponent;

    Task<IDialogReference> ShowAsync<T>(string title, DialogParameters parameters, DialogOptions options) where T : IComponent;
}