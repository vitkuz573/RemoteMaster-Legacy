// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Components.Library;

public class DialogResult
{
    public object Data { get; }

    public Type DataType { get; }

    public bool Canceled { get; }

    protected internal DialogResult(object data, Type resultType, bool canceled)
    {
        Data = data;
        DataType = resultType;
        Canceled = canceled;
    }

    public static DialogResult Ok<T>(T result) => Ok(result, default);

    public static DialogResult Ok<T>(T result, Type dialogType) => new(result, dialogType, false);

    public static DialogResult Cancel() => new(default, typeof(object), true);
}