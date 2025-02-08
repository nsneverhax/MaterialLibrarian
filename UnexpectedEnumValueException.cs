namespace MaterialLibrarian;

public class UnexpectedEnumValueException<T> : Exception
{
    public UnexpectedEnumValueException(int value) : base($"Value \"{value}\" is not defined for type: \"{typeof(T).Name}\"!")
    {

    }
}
