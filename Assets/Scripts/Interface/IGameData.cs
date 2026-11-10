public interface IGameData
{
    int Id { get; }
}

public interface IValidatable
{
    bool Validate();
}
