namespace ReactionLab.Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IStronglyTypedId<TId>
{
    protected Entity(TId id) => Id = id;

    protected Entity()
    {

    }

    public TId Id { get; private init; }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType() && Id.Equals(other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
