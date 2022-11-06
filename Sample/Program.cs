// See https://aka.ms/new-console-template for more information
using Medoz.Docker;
Console.WriteLine("Hello, World!");


foreach( var i in Image.ImagesAsync().Result)
{
    Console.WriteLine(i.ToString());
}
