namespace Medoz.Docker;

public class Spliter
{
    private List<string> _words = new();
    private List<int> _indexes = new();
    public Spliter() { }

    public Spliter(IEnumerable<string> words)
    {
        _words.AddRange(words);
    }

    public Spliter(IEnumerable<string> words, string header)
    {
        _words.AddRange(words);
        SetHeader(header);
    }


    public Spliter AddWord(string word)
    {
        _words.Add(word);
        return this;
    }

    public Spliter SetHeader(string header)
    {
        _indexes.Clear();

        foreach(var word in _words)
        {
            int i = header.IndexOf(word);
            if (i < 0)
            {
                _words.Remove(word);
            }
            else
            {
                _indexes.Add(i);
            }
        }
        return this;
    }

    public IEnumerable<string> Split(string text)
    {
        List<string> split = new();
        if(_indexes.Count() == 0) 
        {
            return split;
        }
        for(int i = 0; i < _indexes.Count() - 1; i++)
        {
            split.Add(text.Substring(_indexes[i], _indexes[i + 1] - _indexes[i] - 1).Trim());
        }
        split.Add(text.Substring(_indexes.Last()));
        return split;
    }

}
