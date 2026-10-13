
namespace DirectoryService.Shared;

public readonly struct CreatedObjectGuid
{
    private readonly Guid _value;
    public Guid Value {  get { return _value; } }
    public CreatedObjectGuid(Guid value)
    {
        _value = value;
    }
    public static implicit operator CreatedObjectGuid(Guid value) => new(value);
    public static implicit operator Guid(CreatedObjectGuid value) => value.Value;
}
