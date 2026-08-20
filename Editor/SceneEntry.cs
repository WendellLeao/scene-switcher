namespace WendellLeao.SceneSwitcher.Editor
{
    internal readonly struct SceneEntry
    {
        public string Guid { get; }
        public string Path { get; }
        public string Name { get; }

        public SceneEntry(string guid, string path, string name)
        {
            Guid = guid;
            Path = path;
            Name = name;
        }
    }
}
