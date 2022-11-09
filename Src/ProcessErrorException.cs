// Copyright (c) 2020 Cysharp, Inc.
// Released under the MIT license
// https://github.com/Cysharp/ProcessX/blob/master/LICENSE

using System;

namespace Medoz.Pmet;

public class ProcessErrorException : Exception
{
    public int ExitCode { get; }
    public string[] ErrorOutput { get; }

    public ProcessErrorException(int exitCode, string[] errorOutput)
        : base($"Process returns error, ExitCode:{exitCode}{Environment.NewLine}{string.Join(Environment.NewLine, errorOutput)}")
    {
        ExitCode = exitCode;
        ErrorOutput = errorOutput;
    }
}
