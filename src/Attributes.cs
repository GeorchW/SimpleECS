namespace SimpleECS
{
    [System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class BannedAttribute : System.Attribute
    {
    }
    [System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class GlobalAttribute : System.Attribute
    {
    }
    [System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class ChangedAttribute : System.Attribute
    {
    }
}
