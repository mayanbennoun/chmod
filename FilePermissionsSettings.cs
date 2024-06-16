namespace chmodPermissions
{
	/// <summary>
	/// Class that is used to store file permission Settings 
	/// </summary>
	public class FilePermissionsSettings
	{
		public int ReadIndex { get; set; } 
		public int WriteIndex { get; set; }
		public string DefaultPermissions { get; set; } // Default file permissions in unix format
	}
}
