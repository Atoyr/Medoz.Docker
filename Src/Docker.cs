using System;

namespace Medoz.Docker;

public class Docker
{
    private Docker()
    {

    }

    protected static Task<(string standardOutput, string errorOutput)> ExecuteAsync(string command, string args)
    {
        return Task.Run(() => ExecuteAsync(command, args, new string[0]));
    }

    protected static Task<(string standardOutput, string errorOutput)> ExecuteAsync(string command, string args, string[] inputs)
    {
        return Task.Run(() => Execute(command, args, inputs));
    }

    protected static (string standardOutput, string errorOutput) Execute(string command, string args)
    {
        return Execute(command, args, new string[0]);
    }

    protected static (string standardOutput, string errorOutput) Execute(string command, string args, string[] inputs)
    {
        System.Text.StringBuilder so = new();
        System.Text.StringBuilder eo = new();

        System.Diagnostics.ProcessStartInfo si = new(command, args);
        si.RedirectStandardError = true;
        si.RedirectStandardOutput = true;
        si.RedirectStandardInput = inputs.Length > 0;
        si.UseShellExecute = false;

        using(System.Diagnostics.Process p = new())
            using(System.Threading.CancellationTokenSource ctoken = new())
            {
                p.EnableRaisingEvents = true;
                p.StartInfo = si;

                p.OutputDataReceived += (sender, ev) =>
                {
                    so.AppendLine(ev.Data);
                };
                p.ErrorDataReceived += (sender, ev) =>
                {
                    if(!string.IsNullOrEmpty(ev.Data))
                    {
                        eo.AppendLine(ev.Data);
                    }
                };
                p.Exited += (sender, ev) =>
                {
                    // プロセスが終了すると呼ばれる
                    ctoken.Cancel();
                };

                try
                {
                    // プロセスの開始
                    p.Start();
                }
                catch(Exception e)
                {
                    eo.AppendLine(e.Message);
                    ctoken.Cancel();
                    return (so.ToString(), eo.ToString());
                }

                if (0 < inputs.Count())
                {
                    StreamWriter sw = p.StandardInput;
                    foreach(string str in inputs)
                    {
                        sw.WriteLine(str);
                    }
                    sw.Close();
                }

                // 非同期出力読出し開始
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                // 終了まで待つ
                ctoken.Token.WaitHandle.WaitOne();
            }
        return (so.ToString(), eo.ToString());
    }

    public static async Task<bool> CanExecuteAsync()
    {
        (string _, string e) = await Docker.ExecuteAsync("docker", "");
        return string.IsNullOrEmpty(e);
    }

//     public static IEnumerable<Image> ImageList()
//     {
//         (string o, string e) = Docker.Execute("docker", "image ls", new string[0]);
//         if (!string.IsNullOrEmpty(e))
//         {
//             throw new Exception(e);
//         }
//         List<Image> images = new();
//         if (string.IsNullOrEmpty(o))
//         {
//             return images;
//         }
// 
//         string[] lines = o.Replace("\r\n", "\n").Split(new[]{'\n','\r'});
//         if(lines.Length < 2)
//         {
//             return images;
//         }
// 
//         Spliter sp = new();
//         sp.AddWord("REPOSITORY");
//         sp.AddWord("TAG");
//         sp.AddWord("IMAGE ID");
//         sp.AddWord("CREATED");
//         sp.AddWord("SIZE");
//         sp.SetHeader(lines[0]);
// 
//         for(int i = 1; i < lines.Length; i++)
//         {
//             if (string.IsNullOrEmpty(lines[i])) continue;
//             Image image = new();
//             IEnnumerable<string> data = sp.Split(lines[i]);
// 
//             if(data.Length == 5)
//             {
//                 image.Repository = data[0];
//                 image.Tag = data[1];
//                 image.ImageId = data[2];
//                 image.Created = data[3];
//                 image.Size = data[4];
//             }
//             images.Add(image);
//         }
// 
//         return images;
//     }
// 
//     public static IEnumerable<string> ProcessList()
//     {
// 
//     }
// 
    // public async Task<(string standardOutput, string errorOutput)> Pull(string imageName)
    // {

    // }

    // public async Task<(string standardOutput, string errorOutput)> Run(string containerName)
    // {

    // }

}
