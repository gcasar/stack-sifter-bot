namespace StackSifter;

public class AllMatchPostSifter : IPostSifter
{
    public bool IsMatch(Feed.Post post) => true;
}
