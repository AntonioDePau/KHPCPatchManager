public class ListBoxItem
{
    public string ShortName { get; set; }
    public string Path { get; set; }
	
    public ListBoxItem(string a, string b)
    {
        ShortName = a;
        Path = b;
    }
}
