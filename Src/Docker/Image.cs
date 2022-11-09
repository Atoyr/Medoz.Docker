using System.Collections.Generic;

namespace Medoz.Pmet.Docker;

public class Image
{
    public Image()
    {
    }

    public string Id { set; get; } = string.Empty;
    public string Repository { set; get; } = string.Empty;
    public string Tag { set; get; } = string.Empty;
    public string Digest { set; get; } = string.Empty;
    public string CreatedSince { set; get; } = string.Empty;
    public string CreatedAt { set; get; } = string.Empty;
    public string Size { set; get; } = string.Empty;

    public static IEnumerable<Image> Images()
    {
        return Image.ImagesAsync().Result;
    }

    public static async Task<IEnumerable<Image>> ImagesAsync()
    {
        List<Image> images = new();

        await foreach(string item in ProcessExecutor.ExecuteAsync("docker images --format \"{{.ID}}\\t{{.Repository}}\\t{{.Tag}}\\t{{.Digest}}\\t{{.CreatedSince}}\\t{{.CreatedAt}}\\t{{.Size}}\""))
        {
            string[] split = item.Split("\t");
            if (split.Length == 7)
            {
                Image image = new();
                image.Id = split[0].Trim();
                image.Repository = split[1].Trim();
                image.Tag = split[2].Trim();
                image.Digest = split[3].Trim();
                image.CreatedSince = split[4].Trim();
                image.CreatedAt = split[5].Trim();
                image.Size = split[6].Trim();
                images.Add(image);
            }
        }

        return images;
    }

    public static IEnumerable<string> Rmi(string target, bool force = false)
    {
        return Image.RmiAsync(target, force).Result;
    }

    public static async Task<IEnumerable<string>> RmiAsync(string target, bool force = false)
    {
        if(target.IndexOf(" ") >= 0)
        {
            throw new Exception("target is wrong word");
        }
        string command = force ? $"docker rmi -f {target}" : $"docker rmi {target}";

        List<string> ret = new();

        await foreach(string line in ProcessExecutor.ExecuteAsync(command))
        {
            ret.Add(line);
        }

        return ret;
    }

    public override string ToString() => $"{Id}\t{Repository}\t{Tag}\t{Digest}\t{CreatedAt}\t{Size}";
}
