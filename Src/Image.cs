using System.Collections.Generic;

namespace Medoz.Docker;

public class Image
{
    public Image()
    {
    }

    public static async Task<IEnumerable<Image>> ImagesAsync()
    {
        List<Image> images = new();

        await foreach(string item in ProcessExecutor.ExecuteAsync("docker images --format \"{{.ID}}\\t{{.Repository}}\\t{{.Tag}}\\t{{.Digest}}\\t{{.CreatedSince}}\\t{{CreatedAt}}\\t{{.Size}}\""))
        {
            string[] split = item.Split("\t");
            if (split.Length == 7)
            {
                Image image = new();
                image.Id = split[0];
                image.Repository = split[1];
                image.Tag = split[2];
                image.Digest = split[3];
                image.CreatedSince = split[4];
                image.CreatedAt = split[5];
                image.Size = split[6];
                images.Add(image);
            }
        }

        return images;
    }

    public string Id { set; get; } = string.Empty;
    public string Repository { set; get; } = string.Empty;
    public string Tag { set; get; } = string.Empty;
    public string Digest { set; get; } = string.Empty;
    public string CreatedSince { set; get; } = string.Empty;
    public string CreatedAt { set; get; } = string.Empty;
    public string Size { set; get; } = string.Empty;

    public override string ToString() => $"{Id}\t{Repository}\t{Tag}\t{Digest}\t{CreatedAt}\t{Size}";
}
