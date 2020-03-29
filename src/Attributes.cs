namespace SimpleECS
{
    [System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    sealed class BannedAttribute : System.Attribute
    {
    }
    [System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    sealed class GlobalAttribute : System.Attribute
    {
    }
    [System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    sealed class ChangedAttribute : System.Attribute
    {
    }
}
