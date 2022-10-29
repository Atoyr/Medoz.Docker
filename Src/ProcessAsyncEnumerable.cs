// Copyright (c) 2020 Cysharp, Inc.
// Released under the MIT license
// https://github.com/Cysharp/ProcessX/blob/master/LICENSE

namespace Medoz.Docker;

using System.Threading.Channels;
using System.Diagnostics;

public class ProcessAsyncEnumerable : IAsyncEnumerable<string>
{
    readonly Process? _process;
    readonly ChannelReader<string> _channel;

    internal ProcessAsyncEnumerable(Process? process, ChannelReader<string> channel)
    {
        _process = process;
        _channel = channel;
    }

    public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new ProcessAsyncEnumerator(_process, _channel, cancellationToken);
    }

    /// <summary>
    /// Consume all result and wait complete asynchronously.
    /// </summary>
    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var _ in this.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
        }
    }

    /// <summary>
    /// Returning first value and wait complete asynchronously.
    /// </summary>
    public async Task<string> FirstAsync(CancellationToken cancellationToken = default)
    {
        string? data = null;
        await foreach (var item in this.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (data is null)
            {
                data = (item ?? "");
            }
        }

        if (data is null)
        {
            throw new InvalidOperationException("Process does not return any data.");
        }
        else
        {
            return data;
        }
    }

    /// <summary>
    /// Returning first value or null and wait complete asynchronously.
    /// </summary>
    public async Task<string?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        string? data = null;
        await foreach (var item in this.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (data is null)
            {
                data = (item ?? "");
            }
        }
        return data;
    }

    public async Task<string[]> ToTask(CancellationToken cancellationToken = default)
    {
        List<string> list = new();
        await foreach (var item in this.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            list.Add(item);
        }
        return list.ToArray();
    }

    /// <summary>
    /// Write the all received data to console.
    /// </summary>
    public async Task WriteLineAllAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var item in this.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine(item);
        }
    }

}
