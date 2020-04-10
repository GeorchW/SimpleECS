namespace SimpleECS
{
    public struct NameComp : ISerializableComponent
    {
        public string Name;
        public NameComp(string name) => Name = name;
    }
}
