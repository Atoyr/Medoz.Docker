using System;

namespace Medoz.Docker;

public class ImageConfig
{
    public ImageConfig()
    {

    }

    public int RepositoryIndex { set; get; }
    public int TagIndex { set; get; }
    public int ImageIdIndex { set; get; }
    public int CreatedIndex { set; get; }
    public int SizeIndex { set; get; }
    public readonly string RepositoryHeaderName = "REPOSITORY";
    public readonly string TagHeaderName = "TAG";
    public readonly string ImageIdName = "IMAGE ID";
    public readonly string CreatedName = "CREATED";
    public readonly string SizeName = "SIZE";
}
