
public enum ParryResult
{
    Parried,
    NotParried
}

public interface IParryResponse
{
    void OnParryResult(ParryResult result);
}
