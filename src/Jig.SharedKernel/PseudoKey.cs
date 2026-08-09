namespace Jig.SharedKernel;

public readonly record struct PseudoKey(Guid Value)
{
    public static PseudoKey New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
