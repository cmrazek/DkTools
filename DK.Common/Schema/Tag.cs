namespace DK.Schema
{
	public class Tag
	{
		public string Name { get; private set; }
		public string Value { get; private set; }

		public Tag(string name, string value)
		{
			Name = name;
			Value = value;
		}
	}
}
