// Copyright (c) 2020 Cysharp, Inc.
// Released under the MIT license
// https://github.com/Cysharp/ProcessX/blob/master/LICENSE

namespace Medoz.Pmet;

using System.Threading.Channels;
using System.Diagnostics;

class ProcessAsyncEnumerator : IAsyncEnumerator<string>
{
    readonly Process? _process;
    readonly ChannelReader<string> _channel;
    readonly CancellationToken _cancellationToken;
    readonly CancellationTokenRegistration _cancellationTokenRegistration;
    string? _current;
    bool _disposed;

    public ProcessAsyncEnumerator(Process? process, ChannelReader<string> channel, CancellationToken cancellationToken)
    {
        // process is not null, kill when canceled.
        _process = process;
        _channel = channel;
        _cancellationToken = cancellationToken;
        if (_cancellationToken.CanBeCanceled)
        {
            _cancellationTokenRegistration = _cancellationToken.Register(() => DisposeAsync());
        }
    }

#pragma warning disable CS8603
    // when call after MoveNext, current always not null.
    public string Current => _current;
#pragma warning restore CS8603

    public async ValueTask<bool> MoveNextAsync()
    {
        if (_channel.TryRead(out _current))
        {
            return true;
        }
        else
        {
            if (await _channel.WaitToReadAsync(_cancellationToken).ConfigureAwait(false))
            {
                if (_channel.TryRead(out _current))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            try
            {
                _cancellationTokenRegistration.Dispose();
                if (_process is not null)
                {
                    _process.EnableRaisingEvents = false;
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                    }
                }
            }
            finally
            {
                if (_process is not null)
                {
                    _process.Dispose();
                }
            }
        }

        return default;
    }
}
