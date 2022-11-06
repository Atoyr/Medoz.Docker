// Copyright (c) 2020 Cysharp, Inc.
// Released under the MIT license
// https://github.com/Cysharp/ProcessX/blob/master/LICENSE

using System.Text;
using System.Diagnostics;
using System.Threading.Channels;

namespace Medoz.Docker;

public static class ProcessExecutor
{
    public static IReadOnlyList<int> AcceptableExitCodes { get; set; } = new[] { 0 };

    static bool IsInvalidExitCode(Process process)
    {
        return !AcceptableExitCodes.Any(x => x == process.ExitCode);
    }

    static (string fileName, string? arguments) ParseCommand(string command)
    {
        string[] split = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return split.Length < 2 ? (command, null) : (split[0], split[1]);
    }

    static Process SetupRedirectableProcess(ref ProcessStartInfo processStartInfo, bool redirectStandardInput)
    {
        // override setings.
        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = true;
        processStartInfo.ErrorDialog = false;
        processStartInfo.RedirectStandardError = true;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardInput = redirectStandardInput;

        Process process = new()
        {
            StartInfo = processStartInfo,
                      EnableRaisingEvents = true,
        };

        return process;
    }

    public static ProcessAsyncEnumerable ExecuteAsync(string command, string? workingDirectory = null, IDictionary<string, string>? environmentVariable = null, Encoding? encoding = null)
    {
        var (fileName, arguments) = ParseCommand(command);
        return ExecuteAsync(fileName, arguments, workingDirectory, environmentVariable, encoding);
    }

    public static ProcessAsyncEnumerable ExecuteAsync(string fileName, string? arguments, string? workingDirectory = null, IDictionary<string, string>? environmentVariable = null, Encoding? encoding = null)
    {
        ProcessStartInfo pi = new()
        {
            FileName = fileName,
                     Arguments = arguments,
        };

        if (workingDirectory is not null)
        {
            pi.WorkingDirectory = workingDirectory;
        }

        if (environmentVariable is not null)
        {
            foreach (var item in environmentVariable)
            {
                pi.EnvironmentVariables[item.Key] = item.Value;
            }
        }

        if (encoding is not null)
        {
            pi.StandardOutputEncoding = encoding;
            pi.StandardErrorEncoding = encoding;
        }

        return ExecuteAsync(pi);
    }

    public static ProcessAsyncEnumerable ExecuteAsync(ProcessStartInfo processStartInfo)
    {
        var process = SetupRedirectableProcess(ref processStartInfo, false);

        var outputChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
                {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true
                });

        List<string> errorList = new();
        TaskCompletionSource<object?> waitOutputDataCompleted = new();

        void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is not null)
            {
                outputChannel?.Writer.TryWrite(e.Data);
            }
            else
            {
                waitOutputDataCompleted?.TrySetResult(null);
            }
        }

        process.OutputDataReceived += OnOutputDataReceived;

        TaskCompletionSource<object?> waitErrorDataCompleted = new();
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                lock (errorList)
                {
                    errorList.Add(e.Data);
                }
            }
            else
            {
                waitErrorDataCompleted.TrySetResult(null);
            }
        };

        process.Exited += async (sender, e) =>
        {
            await waitErrorDataCompleted.Task.ConfigureAwait(false);

            if (errorList.Count == 0)
            {
                await waitOutputDataCompleted.Task.ConfigureAwait(false);
            }
            else
            {
                process.OutputDataReceived -= OnOutputDataReceived;
            }

            if (IsInvalidExitCode(process))
            {
                outputChannel.Writer.TryComplete(new ProcessErrorException(process.ExitCode, errorList.ToArray()));
            }
            else
            {
                if (errorList.Count == 0)
                {
                    outputChannel.Writer.TryComplete();
                }
                else
                {
                    outputChannel.Writer.TryComplete(new ProcessErrorException(process.ExitCode, errorList.ToArray()));
                }
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Can't start process. FileName:" + processStartInfo.FileName + ", Arguments:" + processStartInfo.Arguments);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return new ProcessAsyncEnumerable(process, outputChannel.Reader);
    }

    public static (Process Process, ProcessAsyncEnumerable StdOut, ProcessAsyncEnumerable StdError) GetDualAsyncEnumerable(string command, string? workingDirectory = null, IDictionary<string, string>? environmentVariable = null, Encoding? encoding = null)
    {
        var (fileName, arguments) = ParseCommand(command);
        return GetDualAsyncEnumerable(fileName, arguments, workingDirectory, environmentVariable, encoding);
    }

    public static (Process Process, ProcessAsyncEnumerable StdOut, ProcessAsyncEnumerable StdError) GetDualAsyncEnumerable(string fileName, string? arguments, string? workingDirectory = null, IDictionary<string, string>? environmentVariable = null, Encoding? encoding = null)
    {
        ProcessStartInfo pi = new()
        {
            FileName = fileName,
                     Arguments = arguments,
        };

        if (workingDirectory is not null)
        {
            pi.WorkingDirectory = workingDirectory;
        }

        if (environmentVariable is not null)
        {
            foreach (var item in environmentVariable)
            {
                pi.EnvironmentVariables.Add(item.Key, item.Value);
            }
        }

        if (encoding is not null)
        {
            pi.StandardOutputEncoding = encoding;
            pi.StandardErrorEncoding = encoding;
        }

        return GetDualAsyncEnumerable(pi);
    }

    public static (Process Process, ProcessAsyncEnumerable StdOut, ProcessAsyncEnumerable StdError) GetDualAsyncEnumerable(ProcessStartInfo processStartInfo)
    {
        var process = SetupRedirectableProcess(ref processStartInfo, true);

        var outputChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
                {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true
                });

        var errorChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
                {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true
                });

        var waitOutputDataCompleted = new TaskCompletionSource<object?>();
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputChannel.Writer.TryWrite(e.Data);
            }
            else
            {
                waitOutputDataCompleted.TrySetResult(null);
            }
        };

        var waitErrorDataCompleted = new TaskCompletionSource<object?>();
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorChannel.Writer.TryWrite(e.Data);
            }
            else
            {
                waitErrorDataCompleted.TrySetResult(null);
            }
        };

        process.Exited += async (sender, e) =>
        {
            await waitErrorDataCompleted.Task.ConfigureAwait(false);
            await waitOutputDataCompleted.Task.ConfigureAwait(false);

            if (IsInvalidExitCode(process))
            {
                errorChannel.Writer.TryComplete();
                outputChannel.Writer.TryComplete(new ProcessErrorException(process.ExitCode, Array.Empty<string>()));
            }
            else
            {
                errorChannel.Writer.TryComplete();
                outputChannel.Writer.TryComplete();
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Can't start process. FileName:" + processStartInfo.FileName + ", Arguments:" + processStartInfo.Arguments);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // error itertor does not handle process itself.
        return (process, new ProcessAsyncEnumerable(process, outputChannel.Reader), new ProcessAsyncEnumerable(null, errorChannel.Reader));
    }

    // Binary

    public static Task<byte[]> StartReadBinaryAsync(string command, string? workingDirectory = null, IDictionary<string, string>? environmentVariable = null, Encoding? encoding = null)
    {
        var (fileName, arguments) = ParseCommand(command);
        return StartReadBinaryAsync(fileName, arguments, workingDirectory, environmentVariable, encoding);
    }

    public static Task<byte[]> StartReadBinaryAsync(string fileName, string? arguments, string? workingDirectory = null, IDictionary<string, string>? environmentVariable = null, Encoding? encoding = null)
    {
        var pi = new ProcessStartInfo()
        {
            FileName = fileName,
                     Arguments = arguments,
        };

        if (workingDirectory != null)
        {
            pi.WorkingDirectory = workingDirectory;
        }

        if (environmentVariable != null)
        {
            foreach (var item in environmentVariable)
            {
                pi.EnvironmentVariables.Add(item.Key, item.Value);
            }
        }

        if (encoding != null)
        {
            pi.StandardOutputEncoding = encoding;
            pi.StandardErrorEncoding = encoding;
        }

        return StartReadBinaryAsync(pi);
    }

    public static Task<byte[]> StartReadBinaryAsync(ProcessStartInfo processStartInfo)
    {
        var process = SetupRedirectableProcess(ref processStartInfo, false);

        var errorList = new List<string>();

        var cts = new CancellationTokenSource();
        var resultTask = new TaskCompletionSource<byte[]>();
        var readTask = new TaskCompletionSource<byte[]?>();

        var waitErrorDataCompleted = new TaskCompletionSource<object?>();
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                lock (errorList)
                {
                    errorList.Add(e.Data);
                }
            }
            else
            {
                waitErrorDataCompleted.TrySetResult(null);
            }
        };

        process.Exited += async (sender, e) =>
        {
            await waitErrorDataCompleted.Task.ConfigureAwait(false);

            if (errorList.Count == 0 && !IsInvalidExitCode(process))
            {
                var resultBin = await readTask.Task.ConfigureAwait(false);
                if (resultBin != null)
                {
                    resultTask.TrySetResult(resultBin);
                    return;
                }
            }

            cts.Cancel();

            resultTask.TrySetException(new ProcessErrorException(process.ExitCode, errorList.ToArray()));
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Can't start process. FileName:" + processStartInfo.FileName + ", Arguments:" + processStartInfo.Arguments);
        }

        RunAsyncReadFully(process.StandardOutput.BaseStream, readTask, cts.Token);
        process.BeginErrorReadLine();

        return resultTask.Task;
    }

    static async void RunAsyncReadFully(Stream stream, TaskCompletionSource<byte[]?> completion, CancellationToken cancellationToken)
    {
        try
        {
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms, 81920, cancellationToken);
            var result = ms.ToArray();
            completion.TrySetResult(result);
        }
        catch
        {
            completion.TrySetResult(null);
        }
    }
}
