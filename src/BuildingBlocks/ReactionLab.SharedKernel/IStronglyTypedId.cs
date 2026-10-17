namespace ReactionLab.SharedKernel;

public interface IStronglyTypedId<TSelf>
    where TSelf : IStronglyTypedId<TSelf>
{
    Guid Value { get; }

    static abstract TSelf From(Guid value);

    static abstract TSelf New();
}
