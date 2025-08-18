namespace ImagesGenerator.Models;

public class Subject
{
    public string Number { get; }
    public string Word { get; }
    
    public Subject(string number, string word)
    {
        Number = number;
        Word = word;
    }
    
    public string FileName => $"{Number}_{Word}.png";
    
    public override string ToString() => $"{Number} - {Word}";
}