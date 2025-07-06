namespace StackSifter;

public class AllMatchPostSifter : IPostSifter
{
    public Task<bool> IsMatch(Feed.Post post) => Task.FromResult(true);
}
